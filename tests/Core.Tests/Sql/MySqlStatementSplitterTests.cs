using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class MySqlStatementSplitterTests
{
    private static IReadOnlyList<string> Split(string sql) =>
        MySqlStatementSplitter.SplitStatements(sql).Select(s => s.Text).ToList();

    [Fact]
    public void Splits_SimpleStatements()
    {
        Assert.Equal(["SELECT 1", "SELECT 2"], Split("SELECT 1; SELECT 2;"));
    }

    [Fact]
    public void IgnoresSemicolonInsideSingleQuotedString()
    {
        Assert.Equal(["SELECT ';' AS s", "SELECT 2"], Split("SELECT ';' AS s; SELECT 2"));
    }

    [Fact]
    public void IgnoresSemicolonInsideDoubleQuotedAndBacktick()
    {
        Assert.Equal(["SELECT \"a;b\"", "SELECT `c;d`"], Split("SELECT \"a;b\"; SELECT `c;d`"));
    }

    [Fact]
    public void IgnoresSemicolonInsideLineComment()
    {
        Assert.Equal(["SELECT 1 -- a; b\nFROM t", "SELECT 2"], Split("SELECT 1 -- a; b\nFROM t; SELECT 2"));
    }

    [Fact]
    public void IgnoresSemicolonInsideBlockComment()
    {
        Assert.Equal(["SELECT 1 /* a; b */ FROM t", "SELECT 2"], Split("SELECT 1 /* a; b */ FROM t; SELECT 2"));
    }

    [Fact]
    public void HandlesEscapedQuoteInsideString()
    {
        Assert.Equal(["SELECT 'a''; b'"], Split("SELECT 'a''; b'"));
    }

    [Fact]
    public void RespectsDelimiterDirective()
    {
        const string sql =
            "DELIMITER $$\n" +
            "CREATE PROCEDURE p() BEGIN SELECT 1; SELECT 2; END$$\n" +
            "DELIMITER ;\n" +
            "SELECT 3;";

        var statements = Split(sql);

        Assert.Equal(2, statements.Count);
        Assert.Contains("CREATE PROCEDURE p() BEGIN SELECT 1; SELECT 2; END", statements[0]);
        Assert.Equal("SELECT 3", statements[1]);
    }

    [Fact]
    public void SkipsEmptyStatements()
    {
        Assert.Equal(["SELECT 1", "SELECT 2"], Split("SELECT 1;;  ; SELECT 2;"));
    }

    [Fact]
    public void StatementAt_FindsStatementUnderCursor()
    {
        const string sql = "SELECT 1; SELECT 2; SELECT 3";

        Assert.Equal("SELECT 1", MySqlStatementSplitter.StatementAt(sql, 3));
        Assert.Equal("SELECT 2", MySqlStatementSplitter.StatementAt(sql, 12));
        Assert.Equal("SELECT 3", MySqlStatementSplitter.StatementAt(sql, sql.Length));
    }
}
