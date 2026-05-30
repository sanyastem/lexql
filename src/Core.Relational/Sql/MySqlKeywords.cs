namespace Lexql.Core.Relational.Sql;

public static class MySqlKeywords
{
    public static readonly IReadOnlyList<string> All =
    [
        "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "HAVING", "LIMIT", "OFFSET",
        "DISTINCT", "AS", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "CROSS JOIN", "ON", "USING",
        "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE", "TRUNCATE",
        "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX", "DATABASE", "SCHEMA",
        "AND", "OR", "NOT", "NULL", "IS", "IN", "LIKE", "BETWEEN", "EXISTS",
        "UNION", "UNION ALL", "CASE", "WHEN", "THEN", "ELSE", "END",
        "ASC", "DESC", "COUNT", "SUM", "AVG", "MIN", "MAX",
        "INT", "VARCHAR", "TEXT", "DATETIME", "TIMESTAMP", "DECIMAL", "BOOLEAN",
        "PRIMARY KEY", "FOREIGN KEY", "REFERENCES", "DEFAULT", "AUTO_INCREMENT", "UNIQUE",
    ];
}
