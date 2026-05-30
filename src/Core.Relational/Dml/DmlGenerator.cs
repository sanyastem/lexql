using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Dml;

public static class DmlGenerator
{
    public static IReadOnlyList<DmlCommand> Generate(IEnumerable<RowChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var commands = new List<DmlCommand>();
        foreach (var change in changes)
        {
            if (change.Kind == RowChangeKind.Update && change.Values.Count == 0)
            {
                continue;
            }

            commands.Add(Generate(change));
        }

        return commands;
    }

    public static DmlCommand Generate(RowChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return change.Kind switch
        {
            RowChangeKind.Insert => Insert(change),
            RowChangeKind.Update => Update(change),
            RowChangeKind.Delete => Delete(change),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };
    }

    private static DmlCommand Insert(RowChange change)
    {
        if (change.Values.Count == 0)
        {
            throw new ArgumentException("Insert requires at least one column value.", nameof(change));
        }

        var parameters = new List<QueryParameter>();
        var columns = new List<string>();
        var placeholders = new List<string>();

        var i = 0;
        foreach (var (column, value) in change.Values)
        {
            var name = $"@p{i++}";
            columns.Add(Quote(column));
            placeholders.Add(name);
            parameters.Add(new QueryParameter(name, value));
        }

        var sql = $"INSERT INTO {Target(change)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", placeholders)})";
        return new DmlCommand(sql, parameters);
    }

    private static DmlCommand Update(RowChange change)
    {
        if (change.Values.Count == 0)
        {
            throw new ArgumentException("Update requires at least one changed column.", nameof(change));
        }

        RequireKey(change);

        var parameters = new List<QueryParameter>();
        var assignments = SetClause(change.Values, "@p", parameters);
        var where = WhereClause(change.Key, parameters);

        var sql = $"UPDATE {Target(change)} SET {string.Join(", ", assignments)} WHERE {string.Join(" AND ", where)}";
        return new DmlCommand(sql, parameters);
    }

    private static DmlCommand Delete(RowChange change)
    {
        RequireKey(change);

        var parameters = new List<QueryParameter>();
        var where = WhereClause(change.Key, parameters);

        var sql = $"DELETE FROM {Target(change)} WHERE {string.Join(" AND ", where)}";
        return new DmlCommand(sql, parameters);
    }

    private static List<string> SetClause(
        IReadOnlyDictionary<string, object?> values, string prefix, List<QueryParameter> parameters)
    {
        var assignments = new List<string>();
        var i = 0;
        foreach (var (column, value) in values)
        {
            var name = $"{prefix}{i++}";
            assignments.Add($"{Quote(column)} = {name}");
            parameters.Add(new QueryParameter(name, value));
        }

        return assignments;
    }

    private static List<string> WhereClause(IReadOnlyDictionary<string, object?> key, List<QueryParameter> parameters)
    {
        var conditions = new List<string>();
        var i = 0;
        foreach (var (column, value) in key)
        {
            var name = $"@k{i++}";
            conditions.Add($"{Quote(column)} = {name}");
            parameters.Add(new QueryParameter(name, value));
        }

        return conditions;
    }

    private static void RequireKey(RowChange change)
    {
        if (change.Key.Count == 0)
        {
            throw new ArgumentException($"{change.Kind} requires a primary key.", nameof(change));
        }
    }

    private static string Target(RowChange change) =>
        change.Schema is null ? Quote(change.Table) : $"{Quote(change.Schema)}.{Quote(change.Table)}";

    private static string Quote(string identifier) => $"`{identifier.Replace("`", "``")}`";
}
