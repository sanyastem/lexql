using Lexql.Core.Relational;

namespace Lexql.Providers.MySql.Metadata;

public static class MySqlSchemaMapper
{
    private const string PrimaryIndexName = "PRIMARY";

    public static RelationalSchema Map(MySqlSchemaRows rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var columnsByTable = rows.Columns
            .GroupBy(c => c.TableName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, MapColumns, StringComparer.Ordinal);

        var primaryKeysByTable = rows.PrimaryKeys
            .GroupBy(p => p.TableName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.OrderBy(p => p.Ordinal).Select(p => p.ColumnName).ToList(),
                StringComparer.Ordinal);

        var indexesByTable = rows.Indexes
            .Where(i => !string.Equals(i.IndexName, PrimaryIndexName, StringComparison.Ordinal))
            .GroupBy(i => i.TableName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, MapIndexes, StringComparer.Ordinal);

        var foreignKeysByTable = rows.ForeignKeys
            .GroupBy(f => f.TableName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, MapForeignKeys, StringComparer.Ordinal);

        var checksByTable = rows.Checks
            .GroupBy(c => c.TableName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CheckConstraintInfo>)g
                    .Select(c => new CheckConstraintInfo(c.ConstraintName, c.CheckClause))
                    .ToList(),
                StringComparer.Ordinal);

        var tables = rows.Tables
            .OrderBy(t => t.TableName, StringComparer.Ordinal)
            .Select(t => new TableInfo(
                t.TableName,
                Lookup(columnsByTable, t.TableName, []),
                Lookup(indexesByTable, t.TableName, []),
                Lookup(foreignKeysByTable, t.TableName, []),
                Lookup(primaryKeysByTable, t.TableName, []),
                Lookup(checksByTable, t.TableName, [])))
            .ToList();

        var views = rows.Views
            .OrderBy(v => v.ViewName, StringComparer.Ordinal)
            .Select(v => new ViewInfo(v.ViewName, v.Definition))
            .ToList();

        var routines = rows.Routines
            .Select(r => new RoutineInfo(r.RoutineName, MapRoutineKind(r.RoutineType), r.Definition))
            .Concat(rows.Triggers.Select(t => new RoutineInfo(t.TriggerName, RoutineKind.Trigger, t.Statement)))
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .ToList();

        return new RelationalSchema(rows.Schema, tables, views, routines);
    }

    private static IReadOnlyList<ColumnInfo> MapColumns(IEnumerable<MySqlColumnRow> columns) =>
        columns
            .OrderBy(c => c.OrdinalPosition)
            .Select(c => new ColumnInfo(
                c.ColumnName,
                c.ColumnType,
                c.IsNullable,
                c.Default,
                IsGenerated(c.Extra),
                NormalizeExpression(c.GenerationExpression)))
            .ToList();

    private static IReadOnlyList<IndexInfo> MapIndexes(IEnumerable<MySqlIndexRow> indexes) =>
        indexes
            .GroupBy(i => i.IndexName, StringComparer.Ordinal)
            .Select(g => new IndexInfo(
                g.Key,
                g.OrderBy(i => i.SequenceInIndex).Select(i => i.ColumnName).ToList(),
                !g.First().NonUnique))
            .ToList();

    private static IReadOnlyList<ForeignKeyInfo> MapForeignKeys(IEnumerable<MySqlForeignKeyRow> foreignKeys) =>
        foreignKeys
            .GroupBy(f => f.ConstraintName, StringComparer.Ordinal)
            .Select(g =>
            {
                var ordered = g.OrderBy(f => f.Ordinal).ToList();
                return new ForeignKeyInfo(
                    g.Key,
                    ordered.Select(f => f.ColumnName).ToList(),
                    ordered[0].ReferencedTable,
                    ordered.Select(f => f.ReferencedColumn).ToList());
            })
            .ToList();

    private static bool IsGenerated(string extra) =>
        extra.Contains("GENERATED", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeExpression(string? expression) =>
        string.IsNullOrWhiteSpace(expression) ? null : expression;

    private static RoutineKind MapRoutineKind(string routineType) =>
        string.Equals(routineType, "FUNCTION", StringComparison.OrdinalIgnoreCase)
            ? RoutineKind.Function
            : RoutineKind.Procedure;

    private static T Lookup<T>(IReadOnlyDictionary<string, T> map, string key, T fallback) =>
        map.TryGetValue(key, out var value) ? value : fallback;
}
