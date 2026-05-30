namespace Lexql.Core.Relational.Sql;

public enum SqlStatementKind
{
    Read,
    Write,
    Other,
}

public static class SqlStatementClassifier
{
    private static readonly HashSet<string> WriteKeywords = new(StringComparer.Ordinal)
    {
        "INSERT", "UPDATE", "DELETE", "REPLACE", "MERGE",
        "CREATE", "ALTER", "DROP", "TRUNCATE", "RENAME",
        "GRANT", "REVOKE", "LOAD", "CALL", "LOCK", "FLUSH", "IMPORT",
    };

    private static readonly HashSet<string> ReadKeywords = new(StringComparer.Ordinal)
    {
        "SELECT", "SHOW", "DESCRIBE", "DESC", "EXPLAIN", "WITH", "USE", "SET", "TABLE", "VALUES",
    };

    public static SqlStatementKind Classify(string statement)
    {
        ArgumentNullException.ThrowIfNull(statement);

        var first = MySqlTokenizer.Tokenize(statement).FirstOrDefault(t => t.Channel == 0);
        if (first is null)
        {
            return SqlStatementKind.Other;
        }

        if (WriteKeywords.Contains(first.Symbol))
        {
            return SqlStatementKind.Write;
        }

        return ReadKeywords.Contains(first.Symbol) ? SqlStatementKind.Read : SqlStatementKind.Other;
    }

    public static bool ContainsWrite(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return MySqlStatementSplitter.SplitStatements(sql)
            .Any(statement => Classify(statement.Text) == SqlStatementKind.Write);
    }
}
