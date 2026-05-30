using Lexql.Core.Relational;
using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class EditableResultAnalyzerTests
{
    private static readonly RelationalSchema Schema = new(
        "shop",
        [
            new TableInfo(
                "users",
                [new ColumnInfo("id", "int", false, null), new ColumnInfo("email", "varchar(255)", true, null)],
                [], [], ["id"], []),
            new TableInfo(
                "order_items",
                [new ColumnInfo("order_id", "int", false, null), new ColumnInfo("item_id", "int", false, null)],
                [], [], ["order_id", "item_id"], []),
            new TableInfo(
                "logs",
                [new ColumnInfo("msg", "text", true, null)],
                [], [], [], []),
        ],
        [],
        []);

    private static EditabilityInfo Analyze(string sql, params string[] columns) =>
        EditableResultAnalyzer.Analyze(sql, columns, Schema);

    [Fact]
    public void SingleTableSelectStar_IsEditable()
    {
        var info = Analyze("SELECT * FROM users", "id", "email");

        Assert.True(info.IsEditable);
        Assert.Equal("users", info.Table);
        Assert.Equal(["id"], info.KeyColumns);
    }

    [Fact]
    public void CompositeKey_AllPresent_IsEditable()
    {
        var info = Analyze("SELECT * FROM order_items WHERE order_id = 1", "order_id", "item_id");

        Assert.True(info.IsEditable);
        Assert.Equal(["order_id", "item_id"], info.KeyColumns);
    }

    [Fact]
    public void MissingKeyColumn_NotEditable()
    {
        var info = Analyze("SELECT email FROM users", "email");

        Assert.False(info.IsEditable);
        Assert.Contains("key", info.Reason);
    }

    [Fact]
    public void Join_NotEditable()
    {
        var info = Analyze("SELECT * FROM users u JOIN order_items o ON u.id = o.order_id", "id", "email");

        Assert.False(info.IsEditable);
        Assert.Contains("join", info.Reason);
    }

    [Fact]
    public void Aggregated_NotEditable()
    {
        var info = Analyze("SELECT email FROM users GROUP BY email", "email");

        Assert.False(info.IsEditable);
    }

    [Fact]
    public void TableWithoutPrimaryKey_NotEditable()
    {
        var info = Analyze("SELECT * FROM logs", "msg");

        Assert.False(info.IsEditable);
        Assert.Contains("primary key", info.Reason);
    }

    [Fact]
    public void NonSelect_NotEditable()
    {
        var info = Analyze("UPDATE users SET email = 'x' WHERE id = 1", "id");

        Assert.False(info.IsEditable);
    }

    [Fact]
    public void MultipleStatements_NotEditable()
    {
        var info = Analyze("SELECT * FROM users; SELECT * FROM logs", "id", "email");

        Assert.False(info.IsEditable);
        Assert.Contains("multiple statements", info.Reason);
    }

    [Fact]
    public void NoFromTable_NotEditable()
    {
        var info = Analyze("SELECT 1", "1");

        Assert.False(info.IsEditable);
    }
}
