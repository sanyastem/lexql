using Lexql.Core.Relational;
using Lexql.Core.Relational.Sql;
using NSubstitute;

namespace Lexql.Core.Tests.Sql;

public class RelationalCompletionProviderTests
{
    private readonly IRelationalSchemaReader _reader = Substitute.For<IRelationalSchemaReader>();

    public RelationalCompletionProviderTests()
    {
        var users = new TableInfo(
            "users",
            [new ColumnInfo("id", "int", false, null), new ColumnInfo("email", "varchar(255)", true, null)],
            [], [], ["id"], []);
        var orders = new TableInfo(
            "orders",
            [new ColumnInfo("id", "int", false, null), new ColumnInfo("total", "decimal(10,2)", false, null)],
            [], [], ["id"], []);

        _reader.LoadAsync("shop", Arg.Any<CancellationToken>())
            .Returns(new RelationalSchema("shop", [users, orders], [new ViewInfo("v_active", "select 1")], []));
    }

    private RelationalCompletionProvider Provider(string? ns = "shop") => new(_reader, ns);

    private async Task<IReadOnlyList<CompletionItem>> Complete(string sql, string? ns = "shop")
    {
        var caret = sql.IndexOf('|');
        return await Provider(ns).GetCompletionsAsync(sql.Replace("|", ""), caret, CancellationToken.None);
    }

    [Fact]
    public async Task AfterFrom_SuggestsTablesAndViews()
    {
        var items = await Complete("SELECT * FROM |");

        Assert.Contains(items, i => i.Label == "users" && i.Kind == CompletionKind.Table);
        Assert.Contains(items, i => i.Label == "orders" && i.Kind == CompletionKind.Table);
        Assert.Contains(items, i => i.Label == "v_active");
    }

    [Fact]
    public async Task AfterAliasDot_SuggestsThatTablesColumns()
    {
        var items = await Complete("SELECT u.| FROM users u");

        Assert.Equal(["id", "email"], items.Select(i => i.Label));
        Assert.All(items, i => Assert.Equal(CompletionKind.Column, i.Kind));
    }

    [Fact]
    public async Task AfterAliasDot_ResolvesCorrectTableAmongJoins()
    {
        var items = await Complete("SELECT * FROM users u JOIN orders o ON u.id = o.| ");

        Assert.Equal(["id", "total"], items.Select(i => i.Label));
    }

    [Fact]
    public async Task UnaliasedTableQualifier_SuggestsColumns()
    {
        var items = await Complete("SELECT * FROM users WHERE users.|");

        Assert.Equal(["id", "email"], items.Select(i => i.Label));
    }

    [Fact]
    public async Task ColumnContext_SuggestsColumnsFromFromTables()
    {
        var items = await Complete("SELECT | FROM users u");

        Assert.Contains(items, i => i.Label == "id" && i.Kind == CompletionKind.Column);
        Assert.Contains(items, i => i.Label == "email");
    }

    [Fact]
    public async Task GeneralContext_SuggestsKeywords()
    {
        var items = await Complete("SEL|");

        Assert.Contains(items, i => i.Label == "SELECT" && i.Kind == CompletionKind.Keyword);
    }

    [Fact]
    public async Task NoCurrentDatabase_NoTables()
    {
        var items = await Complete("SELECT * FROM |", ns: null);

        Assert.Empty(items);
    }
}
