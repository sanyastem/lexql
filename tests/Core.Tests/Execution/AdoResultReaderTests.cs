using System.Data;
using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Execution;

namespace Lexql.Core.Tests.Execution;

public class AdoResultReaderTests
{
    private static DataTable Table(string name)
    {
        var table = new DataTable(name);
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        return table;
    }

    private static async Task<List<IRecord>> Collect(IResultSet resultSet)
    {
        var records = new List<IRecord>();
        await foreach (var record in resultSet.ReadAsync(CancellationToken.None))
        {
            records.Add(record);
        }

        return records;
    }

    [Fact]
    public async Task ReadAll_SingleResultSet_FieldsAndRows()
    {
        var table = Table("users");
        table.Rows.Add(1, "alice");
        table.Rows.Add(2, DBNull.Value);
        using var reader = table.CreateDataReader();

        var result = await AdoResultReader.ReadAllAsync(reader, null, CancellationToken.None);
        var resultSet = Assert.Single(result.ResultSets);

        Assert.Equal(["id", "name"], resultSet.Fields.Select(f => f.Name));
        Assert.Equal(CellKind.Integer, resultSet.Fields[0].Kind);
        Assert.Equal(CellKind.String, resultSet.Fields[1].Kind);

        var records = await Collect(resultSet);
        Assert.Equal(2, records.Count);

        var first = Assert.IsType<CellValue.Scalar>(records[0][0]);
        Assert.Equal(1, first.Value);
        Assert.Equal("alice", Assert.IsType<CellValue.Scalar>(records[0]["name"]).Value);

        var nullCell = Assert.IsType<CellValue.Scalar>(records[1]["name"]);
        Assert.Equal(CellKind.Null, nullCell.Kind);
    }

    [Fact]
    public async Task ReadAll_MultipleResultSets()
    {
        var first = Table("a");
        first.Rows.Add(1, "x");
        var second = Table("b");
        second.Rows.Add(2, "y");
        second.Rows.Add(3, "z");

        using var dataSet = new DataSet();
        dataSet.Tables.Add(first);
        dataSet.Tables.Add(second);
        using var reader = dataSet.CreateDataReader();

        var result = await AdoResultReader.ReadAllAsync(reader, null, CancellationToken.None);

        Assert.Equal(2, result.ResultSets.Count);
        Assert.Single(await Collect(result.ResultSets[0]));
        Assert.Equal(2, (await Collect(result.ResultSets[1])).Count);
    }

    [Fact]
    public async Task ReadAll_AppliesRowLimit()
    {
        var table = Table("big");
        for (var i = 1; i <= 5; i++)
        {
            table.Rows.Add(i, $"row{i}");
        }

        using var reader = table.CreateDataReader();

        var result = await AdoResultReader.ReadAllAsync(reader, rowLimit: 2, CancellationToken.None);
        var records = await Collect(result.ResultSets[0]);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task ReadAll_EmptyResultSet_HasFieldsNoRows()
    {
        var table = Table("empty");
        using var reader = table.CreateDataReader();

        var result = await AdoResultReader.ReadAllAsync(reader, null, CancellationToken.None);
        var resultSet = Assert.Single(result.ResultSets);

        Assert.Equal(2, resultSet.Fields.Count);
        Assert.Empty(await Collect(resultSet));
    }

    [Fact]
    public async Task Record_ByNameLookup_IsCaseInsensitive()
    {
        var table = Table("users");
        table.Rows.Add(7, "bob");
        using var reader = table.CreateDataReader();

        var result = await AdoResultReader.ReadAllAsync(reader, null, CancellationToken.None);
        var record = (await Collect(result.ResultSets[0]))[0];

        Assert.Equal("bob", Assert.IsType<CellValue.Scalar>(record["NAME"]).Value);
        Assert.Throws<KeyNotFoundException>(() => record["missing"]);
    }
}
