namespace Lexql.Core.Relational.Sql;

public sealed record TableReference(string? Schema, string Table, string? Alias);

public static class SqlTableReferences
{
    private static readonly HashSet<string> FromClauseKeywords = new(StringComparer.Ordinal)
    {
        "FROM", "JOIN", "INTO", "UPDATE",
    };

    public static IReadOnlyList<TableReference> Parse(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var tokens = MySqlTokenizer.Tokenize(sql)
            .Where(t => t.Channel == 0)
            .OrderBy(t => t.Start)
            .ToList();
        return Parse(tokens);
    }

    public static IReadOnlyList<TableReference> Parse(IReadOnlyList<SqlToken> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var references = new List<TableReference>();
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!FromClauseKeywords.Contains(tokens[i].Symbol))
            {
                continue;
            }

            var j = i + 1;
            while (j < tokens.Count)
            {
                if (!TryParseTableSpec(tokens, j, out var reference, out var next))
                {
                    break;
                }

                references.Add(reference);
                j = next;

                if (j < tokens.Count && tokens[j].Symbol == "COMMA")
                {
                    j++;
                    continue;
                }

                break;
            }

            i = j - 1;
        }

        return references;
    }

    private static bool TryParseTableSpec(
        IReadOnlyList<SqlToken> tokens, int index, out TableReference reference, out int next)
    {
        reference = default!;
        next = index;

        if (index >= tokens.Count || !IsIdentifier(tokens[index]))
        {
            return false;
        }

        string? schema = null;
        var table = tokens[index].Text;
        var k = index + 1;

        if (k < tokens.Count && tokens[k].Symbol == "DOT_ID")
        {
            schema = table;
            table = tokens[k].Text.TrimStart('.');
            k += 1;
        }
        else if (k + 1 < tokens.Count && tokens[k].Symbol == "DOT" && IsIdentifier(tokens[k + 1]))
        {
            schema = table;
            table = tokens[k + 1].Text;
            k += 2;
        }

        string? alias = null;
        if (k < tokens.Count && tokens[k].Symbol == "AS" && k + 1 < tokens.Count && IsIdentifier(tokens[k + 1]))
        {
            alias = tokens[k + 1].Text;
            k += 2;
        }
        else if (k < tokens.Count && IsIdentifier(tokens[k]))
        {
            alias = tokens[k].Text;
            k += 1;
        }

        reference = new TableReference(schema, table, alias);
        next = k;
        return true;
    }

    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "CROSS", "STRAIGHT_JOIN", "NATURAL",
        "ON", "USING", "GROUP", "ORDER", "BY", "HAVING", "LIMIT", "OFFSET", "AS", "AND", "OR", "NOT", "XOR",
        "UNION", "INTO", "UPDATE", "SET", "VALUES", "DISTINCT", "ALL", "DELETE", "INSERT", "CALL", "REPLACE",
    };

    private static bool IsIdentifier(SqlToken token) =>
        token.Symbol == "ID"
        || (IsWordShaped(token.Text) && !ReservedKeywords.Contains(token.Symbol));

    private static bool IsWordShaped(string text) =>
        text.Length > 0
        && (char.IsLetter(text[0]) || text[0] == '_')
        && text.All(c => char.IsLetterOrDigit(c) || c == '_');
}
