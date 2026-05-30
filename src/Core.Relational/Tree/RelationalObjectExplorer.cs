using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Tree;

public sealed class RelationalObjectExplorer : IObjectExplorer
{
    private const char Separator = '\u001F';

    private const string Tables = "tables";
    private const string Views = "views";
    private const string Routines = "routines";

    private readonly IRelationalCatalog _catalog;
    private readonly IRelationalSchemaReader _schemaReader;

    public RelationalObjectExplorer(IRelationalCatalog catalog, IRelationalSchemaReader schemaReader)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(schemaReader);
        _catalog = catalog;
        _schemaReader = schemaReader;
    }

    public async Task<IReadOnlyList<DatabaseObjectNode>> GetRootsAsync(CancellationToken ct)
    {
        var namespaces = await _catalog.ListNamespacesAsync(ct);
        return namespaces
            .Select(ns => new DatabaseObjectNode(
                Encode("ns", ns), ns, DatabaseObjectKind.Namespace, HasChildren: true))
            .ToList();
    }

    public async Task<IReadOnlyList<DatabaseObjectNode>> GetChildrenAsync(DatabaseObjectNode node, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parts = node.Id.Split(Separator);

        return parts[0] switch
        {
            "ns" => NamespaceFolders(parts[1]),
            "folder" => await FolderChildrenAsync(parts[1], parts[2], ct),
            "table" => await TableColumnsAsync(parts[1], parts[2], ct),
            _ => [],
        };
    }

    private static IReadOnlyList<DatabaseObjectNode> NamespaceFolders(string schema) =>
    [
        new(Encode("folder", schema, Tables), "Tables", DatabaseObjectKind.Other, HasChildren: true),
        new(Encode("folder", schema, Views), "Views", DatabaseObjectKind.Other, HasChildren: true),
        new(Encode("folder", schema, Routines), "Routines", DatabaseObjectKind.Other, HasChildren: true),
    ];

    private async Task<IReadOnlyList<DatabaseObjectNode>> FolderChildrenAsync(
        string schema, string category, CancellationToken ct)
    {
        var model = await _schemaReader.LoadAsync(schema, ct);

        return category switch
        {
            Tables => model.Tables
                .Select(t => new DatabaseObjectNode(
                    Encode("table", schema, t.Name), t.Name, DatabaseObjectKind.Container, HasChildren: true))
                .ToList(),
            Views => model.Views
                .Select(v => new DatabaseObjectNode(
                    Encode("view", schema, v.Name), v.Name, DatabaseObjectKind.View, HasChildren: false))
                .ToList(),
            Routines => model.Routines
                .Select(r => new DatabaseObjectNode(
                    Encode("routine", schema, r.Name), r.Name, MapRoutineKind(r.Kind), HasChildren: false))
                .ToList(),
            _ => [],
        };
    }

    private async Task<IReadOnlyList<DatabaseObjectNode>> TableColumnsAsync(
        string schema, string table, CancellationToken ct)
    {
        var model = await _schemaReader.LoadAsync(schema, ct);
        var match = model.Tables.FirstOrDefault(t => string.Equals(t.Name, table, StringComparison.Ordinal));
        if (match is null)
        {
            return [];
        }

        return match.Columns
            .Select(c => new DatabaseObjectNode(
                Encode("column", schema, table, c.Name), c.Name, DatabaseObjectKind.Field, HasChildren: false))
            .ToList();
    }

    private static DatabaseObjectKind MapRoutineKind(RoutineKind kind) =>
        kind == RoutineKind.Trigger ? DatabaseObjectKind.Trigger : DatabaseObjectKind.Routine;

    private static string Encode(params string[] segments) => string.Join(Separator, segments);
}
