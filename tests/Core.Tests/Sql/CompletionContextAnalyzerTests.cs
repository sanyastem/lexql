using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class CompletionContextAnalyzerTests
{
    private static CompletionContext At(string sql) =>
        CompletionContextAnalyzer.GetCompletionContext(sql.Replace("|", ""), sql.IndexOf('|'));

    [Fact]
    public void EmptyStart_IsKeyword()
    {
        Assert.Equal(CompletionContextKind.Keyword, At("|").Kind);
    }

    [Fact]
    public void AfterFrom_IsTable()
    {
        Assert.Equal(CompletionContextKind.Table, At("SELECT * FROM |").Kind);
    }

    [Fact]
    public void AfterJoin_IsTable()
    {
        Assert.Equal(CompletionContextKind.Table, At("SELECT * FROM a JOIN |").Kind);
    }

    [Fact]
    public void PartialTableNameAfterFrom_IsTable()
    {
        Assert.Equal(CompletionContextKind.Table, At("SELECT * FROM us|").Kind);
    }

    [Fact]
    public void AfterSelect_IsColumn()
    {
        Assert.Equal(CompletionContextKind.Column, At("SELECT |").Kind);
    }

    [Fact]
    public void AfterWhereOperator_IsColumn()
    {
        Assert.Equal(CompletionContextKind.Column, At("SELECT * FROM t WHERE x = |").Kind);
    }

    [Fact]
    public void CommaInSelectList_IsColumn()
    {
        Assert.Equal(CompletionContextKind.Column, At("SELECT a, |").Kind);
    }

    [Fact]
    public void CommaInFromList_IsTable()
    {
        Assert.Equal(CompletionContextKind.Table, At("SELECT * FROM a, |").Kind);
    }

    [Fact]
    public void AfterAliasDot_IsAliasMember()
    {
        var context = At("SELECT u.| FROM users u");

        Assert.Equal(CompletionContextKind.AliasMember, context.Kind);
        Assert.Equal("u", context.Alias);
    }

    [Fact]
    public void PartialColumnAfterAliasDot_IsAliasMember()
    {
        var context = At("SELECT u.na| FROM users u");

        Assert.Equal(CompletionContextKind.AliasMember, context.Kind);
        Assert.Equal("u", context.Alias);
    }

    [Fact]
    public void ParsesTableAliasesFromFromAndJoin()
    {
        var context = At("SELECT * FROM shop.users u JOIN orders AS o ON u.id = o.user_id WHERE |");

        Assert.Contains(context.Tables, t => t.Schema == "shop" && t.Table == "users" && t.Alias == "u");
        Assert.Contains(context.Tables, t => t.Table == "orders" && t.Alias == "o");
    }

    [Fact]
    public void TableWithoutAlias_IsParsed()
    {
        var context = At("SELECT * FROM users WHERE |");

        var reference = Assert.Single(context.Tables);
        Assert.Equal("users", reference.Table);
        Assert.Null(reference.Alias);
    }

    [Fact]
    public void ContextIsScopedToStatementUnderCursor()
    {
        var context = At("SELECT * FROM a; SELECT * FROM b WHERE |");

        var reference = Assert.Single(context.Tables);
        Assert.Equal("b", reference.Table);
    }
}
