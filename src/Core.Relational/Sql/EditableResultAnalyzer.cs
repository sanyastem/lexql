namespace Lexql.Core.Relational.Sql;

public sealed record EditabilityInfo(
    bool IsEditable,
    string? Schema,
    string? Table,
    IReadOnlyList<string> KeyColumns,
    string Reason);

public static class EditableResultAnalyzer
{
    public static EditabilityInfo Analyze(string sql, IReadOnlyList<string> resultColumns, RelationalSchema schema)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(resultColumns);
        ArgumentNullException.ThrowIfNull(schema);

        var statements = MySqlStatementSplitter.SplitStatements(sql);
        if (statements.Count != 1)
        {
            return NotEditable("query has multiple statements");
        }

        var tokens = MySqlTokenizer.Tokenize(statements[0].Text)
            .Where(t => t.Channel == 0)
            .ToList();

        if (tokens.Count == 0 || tokens[0].Symbol != "SELECT")
        {
            return NotEditable("not a SELECT query");
        }

        if (tokens.Any(t => t.Symbol == "JOIN"))
        {
            return NotEditable("query joins multiple tables");
        }

        if (tokens.Any(t => t.Symbol is "GROUP" or "DISTINCT" or "UNION"))
        {
            return NotEditable("query is aggregated or combined");
        }

        var references = SqlTableReferences.Parse(tokens);
        if (references.Count != 1)
        {
            return NotEditable("query does not target a single table");
        }

        var tableRef = references[0];
        var table = schema.Tables.FirstOrDefault(t => Eq(t.Name, tableRef.Table));
        if (table is null)
        {
            return NotEditable($"table '{tableRef.Table}' not found in schema");
        }

        if (table.PrimaryKey.Count == 0)
        {
            return NotEditable($"table '{table.Name}' has no primary key");
        }

        var missing = table.PrimaryKey.Where(pk => !resultColumns.Any(c => Eq(c, pk))).ToList();
        if (missing.Count > 0)
        {
            return NotEditable($"result is missing key column(s): {string.Join(", ", missing)}");
        }

        return new EditabilityInfo(true, tableRef.Schema ?? schema.Name, table.Name, table.PrimaryKey, "editable");
    }

    private static EditabilityInfo NotEditable(string reason) => new(false, null, null, [], reason);

    private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
