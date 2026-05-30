using Lexql.Core.Relational.Dml;

namespace Lexql.Core.Tests.Dml;

public class DmlGeneratorTests
{
    private static Dictionary<string, object?> Map(params (string Key, object? Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void Insert_GeneratesParameterizedSql()
    {
        var change = new RowChange(
            RowChangeKind.Insert, "users",
            Map(("email", "a@b.c"), ("active", true)),
            Map());

        var command = DmlGenerator.Generate(change);

        Assert.Equal("INSERT INTO `users` (`email`, `active`) VALUES (@p0, @p1)", command.Sql);
        Assert.Equal(["@p0", "@p1"], command.Parameters.Select(p => p.Name));
        Assert.Equal(["a@b.c", true], command.Parameters.Select(p => p.Value));
    }

    [Fact]
    public void Update_SetsChangedColumns_AndKeyWhere()
    {
        var change = new RowChange(
            RowChangeKind.Update, "users",
            Map(("email", "new@x.y")),
            Map(("id", 7)));

        var command = DmlGenerator.Generate(change);

        Assert.Equal("UPDATE `users` SET `email` = @p0 WHERE `id` = @k0", command.Sql);
        Assert.Equal("new@x.y", command.Parameters[0].Value);
        Assert.Equal(7, command.Parameters[1].Value);
    }

    [Fact]
    public void Delete_UsesCompositeKey()
    {
        var change = new RowChange(
            RowChangeKind.Delete, "order_items",
            Map(),
            Map(("order_id", 1), ("item_id", 2)));

        var command = DmlGenerator.Generate(change);

        Assert.Equal("DELETE FROM `order_items` WHERE `order_id` = @k0 AND `item_id` = @k1", command.Sql);
        Assert.Equal([1, 2], command.Parameters.Select(p => p.Value));
    }

    [Fact]
    public void SchemaQualified_AndIdentifierQuotingEscapes()
    {
        var change = new RowChange(
            RowChangeKind.Delete, "we`ird",
            Map(),
            Map(("id", 1)),
            Schema: "shop");

        var command = DmlGenerator.Generate(change);

        Assert.StartsWith("DELETE FROM `shop`.`we``ird` WHERE", command.Sql);
    }

    [Fact]
    public void Generate_SkipsNoOpUpdates()
    {
        var changes = new[]
        {
            new RowChange(RowChangeKind.Update, "t", Map(), Map(("id", 1))),
            new RowChange(RowChangeKind.Update, "t", Map(("a", 2)), Map(("id", 1))),
        };

        var commands = DmlGenerator.Generate(changes);

        Assert.Single(commands);
    }

    [Fact]
    public void Update_WithoutKey_Throws()
    {
        var change = new RowChange(RowChangeKind.Update, "t", Map(("a", 1)), Map());

        Assert.Throws<ArgumentException>(() => DmlGenerator.Generate(change));
    }

    [Fact]
    public void Insert_WithoutValues_Throws()
    {
        var change = new RowChange(RowChangeKind.Insert, "t", Map(), Map());

        Assert.Throws<ArgumentException>(() => DmlGenerator.Generate(change));
    }
}
