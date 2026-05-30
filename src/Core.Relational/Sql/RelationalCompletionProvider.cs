namespace Lexql.Core.Relational.Sql;

public sealed class RelationalCompletionProvider : ISqlCompletionProvider
{
    private readonly IRelationalSchemaReader _schemaReader;
    private readonly string? _defaultNamespace;

    public RelationalCompletionProvider(IRelationalSchemaReader schemaReader, string? defaultNamespace)
    {
        ArgumentNullException.ThrowIfNull(schemaReader);
        _schemaReader = schemaReader;
        _defaultNamespace = string.IsNullOrWhiteSpace(defaultNamespace) ? null : defaultNamespace;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        string text, int caretOffset, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(text);

        var context = CompletionContextAnalyzer.GetCompletionContext(text, caretOffset);

        return context.Kind switch
        {
            CompletionContextKind.Table => await TablesAsync(ct),
            CompletionContextKind.AliasMember => await AliasColumnsAsync(context, ct),
            CompletionContextKind.Column => await ColumnsAsync(context, ct),
            _ => Keywords(),
        };
    }

    private async Task<IReadOnlyList<CompletionItem>> TablesAsync(CancellationToken ct)
    {
        if (_defaultNamespace is null)
        {
            return [];
        }

        var schema = await _schemaReader.LoadAsync(_defaultNamespace, ct);
        return schema.Tables
            .Select(t => new CompletionItem(t.Name, CompletionKind.Table, "table"))
            .Concat(schema.Views.Select(v => new CompletionItem(v.Name, CompletionKind.Table, "view")))
            .ToList();
    }

    private async Task<IReadOnlyList<CompletionItem>> AliasColumnsAsync(
        CompletionContext context, CancellationToken ct)
    {
        var reference = context.Tables.FirstOrDefault(
            t => Matches(t.Alias, context.Alias) || Matches(t.Table, context.Alias));

        if (reference is null)
        {
            return [];
        }

        var table = await LoadTableAsync(reference, ct);
        return table is null ? [] : Columns(table, reference);
    }

    private async Task<IReadOnlyList<CompletionItem>> ColumnsAsync(CompletionContext context, CancellationToken ct)
    {
        if (context.Tables.Count == 0)
        {
            return Keywords();
        }

        var items = new List<CompletionItem>();
        foreach (var reference in context.Tables)
        {
            var table = await LoadTableAsync(reference, ct);
            if (table is not null)
            {
                items.AddRange(Columns(table, reference));
            }
        }

        return items.Count == 0 ? Keywords() : items;
    }

    private async Task<TableInfo?> LoadTableAsync(TableReference reference, CancellationToken ct)
    {
        var namespaceName = reference.Schema ?? _defaultNamespace;
        if (namespaceName is null)
        {
            return null;
        }

        var schema = await _schemaReader.LoadAsync(namespaceName, ct);
        return schema.Tables.FirstOrDefault(t => Matches(t.Name, reference.Table));
    }

    private static IReadOnlyList<CompletionItem> Columns(TableInfo table, TableReference reference)
    {
        var detail = reference.Alias ?? reference.Table;
        return table.Columns
            .Select(c => new CompletionItem(c.Name, CompletionKind.Column, $"{detail}.{c.Name} : {c.DataType}"))
            .ToList();
    }

    private static IReadOnlyList<CompletionItem> Keywords() =>
        MySqlKeywords.All.Select(k => new CompletionItem(k, CompletionKind.Keyword)).ToList();

    private static bool Matches(string? a, string? b) =>
        a is not null && b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
