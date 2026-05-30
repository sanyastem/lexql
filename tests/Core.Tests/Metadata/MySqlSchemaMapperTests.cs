using Lexql.Core.Relational;
using Lexql.Providers.MySql.Metadata;

namespace Lexql.Core.Tests.Metadata;

public class MySqlSchemaMapperTests
{
    private static MySqlSchemaRows Rows(
        IReadOnlyList<MySqlTableRow>? tables = null,
        IReadOnlyList<MySqlColumnRow>? columns = null,
        IReadOnlyList<MySqlPrimaryKeyRow>? primaryKeys = null,
        IReadOnlyList<MySqlIndexRow>? indexes = null,
        IReadOnlyList<MySqlForeignKeyRow>? foreignKeys = null,
        IReadOnlyList<MySqlCheckRow>? checks = null,
        IReadOnlyList<MySqlViewRow>? views = null,
        IReadOnlyList<MySqlRoutineRow>? routines = null,
        IReadOnlyList<MySqlTriggerRow>? triggers = null) =>
        new("shop", tables ?? [], columns ?? [], primaryKeys ?? [], indexes ?? [],
            foreignKeys ?? [], checks ?? [], views ?? [], routines ?? [], triggers ?? []);

    [Fact]
    public void Map_ColumnsOrderedByOrdinal_WithTypeNullableDefault()
    {
        var rows = Rows(
            tables: [new("users")],
            columns:
            [
                new("users", "name", 2, "varchar(255)", true, null, "", null),
                new("users", "id", 1, "int", false, null, "auto_increment", null),
            ]);

        var schema = MySqlSchemaMapper.Map(rows);
        var columns = schema.Tables.Single().Columns;

        Assert.Equal(["id", "name"], columns.Select(c => c.Name));
        Assert.Equal("int", columns[0].DataType);
        Assert.False(columns[0].Nullable);
        Assert.True(columns[1].Nullable);
    }

    [Fact]
    public void Map_GeneratedColumn_IsDetected()
    {
        var rows = Rows(
            tables: [new("users")],
            columns:
            [
                new("users", "full", 1, "varchar(64)", true, null, "STORED GENERATED", "concat(a,b)"),
            ]);

        var column = MySqlSchemaMapper.Map(rows).Tables.Single().Columns.Single();

        Assert.True(column.IsGenerated);
        Assert.Equal("concat(a,b)", column.GenerationExpression);
    }

    [Fact]
    public void Map_PrimaryKey_OrderedByOrdinal()
    {
        var rows = Rows(
            tables: [new("order_items")],
            primaryKeys:
            [
                new("order_items", "item_id", 2),
                new("order_items", "order_id", 1),
            ]);

        var table = MySqlSchemaMapper.Map(rows).Tables.Single();

        Assert.Equal(["order_id", "item_id"], table.PrimaryKey);
    }

    [Fact]
    public void Map_PrimaryIndex_ExcludedFromIndexes()
    {
        var rows = Rows(
            tables: [new("users")],
            indexes:
            [
                new("users", "PRIMARY", "id", 1, false),
                new("users", "ix_email", "email", 1, false),
            ]);

        var indexes = MySqlSchemaMapper.Map(rows).Tables.Single().Indexes;

        Assert.Single(indexes);
        Assert.Equal("ix_email", indexes[0].Name);
        Assert.True(indexes[0].Unique);
    }

    [Fact]
    public void Map_CompositeIndex_ColumnsOrderedAndUniqueFlag()
    {
        var rows = Rows(
            tables: [new("users")],
            indexes:
            [
                new("users", "ix_name", "last", 2, true),
                new("users", "ix_name", "first", 1, true),
            ]);

        var index = MySqlSchemaMapper.Map(rows).Tables.Single().Indexes.Single();

        Assert.Equal(["first", "last"], index.Columns);
        Assert.False(index.Unique);
    }

    [Fact]
    public void Map_CompositeForeignKey_GroupedAndOrdered()
    {
        var rows = Rows(
            tables: [new("order_items")],
            foreignKeys:
            [
                new("order_items", "fk_order", "order_year", 2, "orders", "year"),
                new("order_items", "fk_order", "order_no", 1, "orders", "no"),
            ]);

        var fk = MySqlSchemaMapper.Map(rows).Tables.Single().ForeignKeys.Single();

        Assert.Equal("fk_order", fk.Name);
        Assert.Equal(["order_no", "order_year"], fk.Columns);
        Assert.Equal("orders", fk.RefTable);
        Assert.Equal(["no", "year"], fk.RefColumns);
    }

    [Fact]
    public void Map_CheckConstraints_Mapped()
    {
        var rows = Rows(
            tables: [new("products")],
            checks: [new("products", "ck_price", "(`price` > 0)")]);

        var check = MySqlSchemaMapper.Map(rows).Tables.Single().Checks.Single();

        Assert.Equal("ck_price", check.Name);
        Assert.Equal("(`price` > 0)", check.Expression);
    }

    [Fact]
    public void Map_ViewsAndRoutinesAndTriggers()
    {
        var rows = Rows(
            views: [new("v_active", "select 1")],
            routines:
            [
                new("get_user", "FUNCTION", "BEGIN END"),
                new("do_stuff", "PROCEDURE", "BEGIN END"),
            ],
            triggers: [new("trg_audit", "INSERT INTO log VALUES (1)")]);

        var schema = MySqlSchemaMapper.Map(rows);

        Assert.Equal("v_active", schema.Views.Single().Name);
        Assert.Equal(RoutineKind.Function, schema.Routines.Single(r => r.Name == "get_user").Kind);
        Assert.Equal(RoutineKind.Procedure, schema.Routines.Single(r => r.Name == "do_stuff").Kind);
        Assert.Equal(RoutineKind.Trigger, schema.Routines.Single(r => r.Name == "trg_audit").Kind);
    }

    [Fact]
    public void Map_TableWithoutMetadata_HasEmptyCollections()
    {
        var schema = MySqlSchemaMapper.Map(Rows(tables: [new("empty")]));
        var table = schema.Tables.Single();

        Assert.Empty(table.Columns);
        Assert.Empty(table.Indexes);
        Assert.Empty(table.ForeignKeys);
        Assert.Empty(table.PrimaryKey);
        Assert.Empty(table.Checks);
    }
}
