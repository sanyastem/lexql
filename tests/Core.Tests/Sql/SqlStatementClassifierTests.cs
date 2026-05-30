using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class SqlStatementClassifierTests
{
    [Theory]
    [InlineData("SELECT * FROM t", SqlStatementKind.Read)]
    [InlineData("SHOW TABLES", SqlStatementKind.Read)]
    [InlineData("EXPLAIN SELECT 1", SqlStatementKind.Read)]
    [InlineData("INSERT INTO t (a) VALUES (1)", SqlStatementKind.Write)]
    [InlineData("UPDATE t SET a = 1", SqlStatementKind.Write)]
    [InlineData("DELETE FROM t", SqlStatementKind.Write)]
    [InlineData("REPLACE INTO t (a) VALUES (1)", SqlStatementKind.Write)]
    [InlineData("CREATE TABLE t (id INT)", SqlStatementKind.Write)]
    [InlineData("ALTER TABLE t ADD b INT", SqlStatementKind.Write)]
    [InlineData("DROP TABLE t", SqlStatementKind.Write)]
    [InlineData("TRUNCATE TABLE t", SqlStatementKind.Write)]
    public void Classify_DetectsReadAndWrite(string sql, SqlStatementKind expected)
    {
        Assert.Equal(expected, SqlStatementClassifier.Classify(sql));
    }

    [Fact]
    public void ContainsWrite_TrueWhenAnyStatementWrites()
    {
        Assert.True(SqlStatementClassifier.ContainsWrite("SELECT 1; DELETE FROM t"));
    }

    [Fact]
    public void ContainsWrite_FalseForAllReads()
    {
        Assert.False(SqlStatementClassifier.ContainsWrite("SELECT 1; SHOW TABLES; SELECT * FROM t"));
    }

    [Fact]
    public void ContainsWrite_IgnoresKeywordInString()
    {
        Assert.False(SqlStatementClassifier.ContainsWrite("SELECT 'DELETE FROM t' AS s"));
    }

    [Fact]
    public void IsAllowedInReadOnly_AllowsOnlyReads()
    {
        Assert.True(SqlStatementClassifier.IsAllowedInReadOnly("SELECT 1; SHOW TABLES"));
    }

    [Fact]
    public void IsAllowedInReadOnly_FailsClosedForUnknownStatements()
    {
        Assert.False(SqlStatementClassifier.IsAllowedInReadOnly("CALL do_something()"));
    }

    [Fact]
    public void IsAllowedInReadOnly_EmptyIsNotAllowed()
    {
        Assert.False(SqlStatementClassifier.IsAllowedInReadOnly("   "));
    }
}
