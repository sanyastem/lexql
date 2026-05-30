using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class SqlStatementSplitterTests
{
    [Fact]
    public void Split_SeparatesOnSemicolons()
    {
        var statements = SqlStatementSplitter.Split("SELECT 1; SELECT 2;");

        Assert.Equal(["SELECT 1", "SELECT 2"], statements.Select(s => s.Text));
    }

    [Fact]
    public void Split_IgnoresSemicolonsInsideStrings()
    {
        var statements = SqlStatementSplitter.Split("SELECT ';' AS s; SELECT 2");

        Assert.Equal(["SELECT ';' AS s", "SELECT 2"], statements.Select(s => s.Text));
    }

    [Fact]
    public void Split_SingleStatementWithoutSemicolon()
    {
        var statements = SqlStatementSplitter.Split("SELECT 1");

        Assert.Equal("SELECT 1", Assert.Single(statements).Text);
    }

    [Fact]
    public void Split_SkipsEmptyStatements()
    {
        var statements = SqlStatementSplitter.Split("SELECT 1;;  ; SELECT 2;");

        Assert.Equal(["SELECT 1", "SELECT 2"], statements.Select(s => s.Text));
    }

    [Fact]
    public void StatementAt_ReturnsStatementUnderCursor()
    {
        const string sql = "SELECT 1; SELECT 2; SELECT 3";

        Assert.Equal("SELECT 1", SqlStatementSplitter.StatementAt(sql, 3));
        Assert.Equal("SELECT 2", SqlStatementSplitter.StatementAt(sql, 12));
        Assert.Equal("SELECT 3", SqlStatementSplitter.StatementAt(sql, sql.Length));
    }

    [Fact]
    public void StatementAt_EmptyText_ReturnsNull()
    {
        Assert.Null(SqlStatementSplitter.StatementAt("", 0));
    }
}
