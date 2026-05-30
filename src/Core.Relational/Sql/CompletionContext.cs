namespace Lexql.Core.Relational.Sql;

public enum CompletionContextKind
{
    Unknown,
    Keyword,
    Table,
    Column,
    AliasMember,
}

public sealed record CompletionContext(
    CompletionContextKind Kind,
    string? Alias,
    IReadOnlyList<TableReference> Tables);

public static class CompletionContextAnalyzer
{
    private static readonly HashSet<string> TableAnchors = new(StringComparer.Ordinal)
    {
        "FROM", "JOIN", "INTO", "UPDATE",
    };

    private static readonly HashSet<string> ColumnAnchors = new(StringComparer.Ordinal)
    {
        "SELECT", "WHERE", "ON", "HAVING", "SET", "AND", "OR", "BY", "VALUES",
    };

    private static readonly HashSet<string> FromClauseKeywords = new(StringComparer.Ordinal)
    {
        "FROM", "JOIN", "INTO", "UPDATE",
    };

    private static readonly HashSet<string> ClauseKeywords = new(StringComparer.Ordinal)
    {
        "SELECT", "FROM", "WHERE", "JOIN", "ON", "GROUP", "ORDER", "HAVING", "SET", "INTO", "UPDATE", "VALUES",
    };

    private static readonly HashSet<string> Operators = new(StringComparer.Ordinal)
    {
        "=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%",
    };

    public static CompletionContext GetCompletionContext(string sql, int caret)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var (start, end) = StatementSpanAt(sql, caret);
        var local = sql[start..end];
        var localCaret = Math.Clamp(caret - start, 0, local.Length);

        var tokens = MySqlTokenizer.Tokenize(local)
            .Where(t => t.Channel == 0)
            .OrderBy(t => t.Start)
            .ToList();

        var tables = SqlTableReferences.Parse(tokens);

        var lastIndex = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Start < localCaret)
            {
                lastIndex = i;
            }
            else
            {
                break;
            }
        }

        if (lastIndex < 0)
        {
            return new CompletionContext(CompletionContextKind.Keyword, null, tables);
        }

        var last = tokens[lastIndex];
        var adjacent = last.Stop + 1 >= localCaret;
        var partial = adjacent && IsIdentifier(last);

        if (adjacent && last.Symbol == "DOT" && TryAlias(tokens, lastIndex - 1, out var dotAlias))
        {
            return new CompletionContext(CompletionContextKind.AliasMember, dotAlias, tables);
        }

        if (adjacent && last.Symbol == "DOT_ID" && TryAlias(tokens, lastIndex - 1, out var memberAlias))
        {
            return new CompletionContext(CompletionContextKind.AliasMember, memberAlias, tables);
        }

        var anchorIndex = partial ? lastIndex - 1 : lastIndex;
        var anchor = anchorIndex >= 0 ? tokens[anchorIndex] : null;

        var kind = Classify(tokens, anchorIndex, anchor);
        return new CompletionContext(kind, null, tables);
    }

    private static CompletionContextKind Classify(IReadOnlyList<SqlToken> tokens, int anchorIndex, SqlToken? anchor)
    {
        if (anchor is null)
        {
            return CompletionContextKind.Keyword;
        }

        if (TableAnchors.Contains(anchor.Symbol))
        {
            return CompletionContextKind.Table;
        }

        if (anchor.Symbol == "COMMA")
        {
            return NearestClauseIsFrom(tokens, anchorIndex)
                ? CompletionContextKind.Table
                : CompletionContextKind.Column;
        }

        if (ColumnAnchors.Contains(anchor.Symbol) || Operators.Contains(anchor.Text))
        {
            return CompletionContextKind.Column;
        }

        return CompletionContextKind.Keyword;
    }

    private static bool NearestClauseIsFrom(IReadOnlyList<SqlToken> tokens, int fromIndex)
    {
        for (var i = fromIndex; i >= 0; i--)
        {
            if (ClauseKeywords.Contains(tokens[i].Symbol))
            {
                return FromClauseKeywords.Contains(tokens[i].Symbol);
            }
        }

        return false;
    }

    private static bool TryAlias(IReadOnlyList<SqlToken> tokens, int index, out string? alias)
    {
        if (index >= 0 && IsIdentifier(tokens[index]))
        {
            alias = tokens[index].Text;
            return true;
        }

        alias = null;
        return false;
    }

    private static bool IsIdentifier(SqlToken token) => token.Symbol == "ID";

    private static (int Start, int End) StatementSpanAt(string sql, int caret)
    {
        foreach (var statement in MySqlStatementSplitter.SplitStatements(sql))
        {
            if (caret >= statement.Start && caret <= statement.End)
            {
                return (statement.Start, statement.End);
            }
        }

        return (0, sql.Length);
    }
}
