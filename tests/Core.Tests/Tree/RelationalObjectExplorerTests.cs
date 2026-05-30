using Lexql.Core.Abstractions;
using Lexql.Core.Relational;
using Lexql.Core.Relational.Tree;
using NSubstitute;

namespace Lexql.Core.Tests.Tree;

public class RelationalObjectExplorerTests
{
    private readonly IRelationalCatalog _catalog = Substitute.For<IRelationalCatalog>();
    private readonly IRelationalSchemaReader _schemaReader = Substitute.For<IRelationalSchemaReader>();

    private RelationalObjectExplorer CreateExplorer() => new(_catalog, _schemaReader);

    private static TableInfo Table(string name, params string[] columns) =>
        new(name, columns.Select(c => new ColumnInfo(c, "int", false, null)).ToList(), [], [], [], []);

    private async Task<IReadOnlyList<DatabaseObjectNode>> ChildrenOf(DatabaseObjectNode node) =>
        await CreateExplorer().GetChildrenAsync(node, CancellationToken.None);

    [Fact]
    public async Task GetRoots_ReturnsNamespaces()
    {
        var roots = await Roots(["shop", "blog"]);

        Assert.Equal(["shop", "blog"], roots.Select(r => r.Name));
        Assert.All(roots, r => Assert.Equal(DatabaseObjectKind.Namespace, r.Kind));
        Assert.All(roots, r => Assert.True(r.HasChildren));
    }

    [Fact]
    public async Task GetChildren_OfNamespace_ReturnsCategoryFolders()
    {
        var ns = (await Roots(["shop"]))[0];

        var folders = await ChildrenOf(ns);

        Assert.Equal(["Tables", "Views", "Routines"], folders.Select(f => f.Name));
        Assert.All(folders, f => Assert.Equal(DatabaseObjectKind.Other, f.Kind));
    }

    [Fact]
    public async Task GetChildren_OfTablesFolder_ReturnsTables()
    {
        _schemaReader.LoadAsync("shop", Arg.Any<CancellationToken>())
            .Returns(new RelationalSchema("shop", [Table("users", "id"), Table("orders", "id")], [], []));
        var tablesFolder = (await ChildrenOf((await Roots(["shop"]))[0]))[0];

        var tables = await ChildrenOf(tablesFolder);

        Assert.Equal(["users", "orders"], tables.Select(t => t.Name));
        Assert.All(tables, t => Assert.Equal(DatabaseObjectKind.Container, t.Kind));
    }

    [Fact]
    public async Task GetChildren_OfViewsAndRoutinesFolders()
    {
        _schemaReader.LoadAsync("shop", Arg.Any<CancellationToken>())
            .Returns(new RelationalSchema(
                "shop",
                [],
                [new ViewInfo("v_active", "select 1")],
                [new RoutineInfo("do_it", RoutineKind.Procedure, "BEGIN END"),
                 new RoutineInfo("trg", RoutineKind.Trigger, "x")]));
        var folders = await ChildrenOf((await Roots(["shop"]))[0]);

        var views = await ChildrenOf(folders[1]);
        var routines = await ChildrenOf(folders[2]);

        Assert.Equal("v_active", Assert.Single(views).Name);
        Assert.Equal(DatabaseObjectKind.View, views[0].Kind);
        Assert.Equal(DatabaseObjectKind.Routine, routines.Single(r => r.Name == "do_it").Kind);
        Assert.Equal(DatabaseObjectKind.Trigger, routines.Single(r => r.Name == "trg").Kind);
    }

    [Fact]
    public async Task GetChildren_OfTable_ReturnsColumns()
    {
        _schemaReader.LoadAsync("shop", Arg.Any<CancellationToken>())
            .Returns(new RelationalSchema("shop", [Table("users", "id", "email")], [], []));
        var tablesFolder = (await ChildrenOf((await Roots(["shop"]))[0]))[0];
        var usersTable = (await ChildrenOf(tablesFolder))[0];

        var columns = await ChildrenOf(usersTable);

        Assert.Equal(["id", "email"], columns.Select(c => c.Name));
        Assert.All(columns, c => Assert.Equal(DatabaseObjectKind.Field, c.Kind));
    }

    private async Task<IReadOnlyList<DatabaseObjectNode>> Roots(IReadOnlyList<string> namespaces)
    {
        _catalog.ListNamespacesAsync(Arg.Any<CancellationToken>()).Returns(namespaces);
        return await CreateExplorer().GetRootsAsync(CancellationToken.None);
    }
}
