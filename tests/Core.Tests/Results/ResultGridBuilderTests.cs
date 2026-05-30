using System.Data;
using Lexql.Core.Relational.Execution;
using Lexql.Core.Results;

namespace Lexql.Core.Tests.Results;

public class ResultGridBuilderTests
{
    private static async Task<ResultGridModel> BuildFrom(DataTable table)
    {
        using var reader = table.CreateDataReader();
        var result = await AdoResultReader.ReadAllAsync(reader, null, CancellationToken.None);
        return await ResultGridBuilder.BuildAsync(result.ResultSets[0], CancellationToken.None);
    }

    [Fact]
    public async Task Build_MapsColumnsWithNumericFlag()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Rows.Add(1, "alice");

        var model = await BuildFrom(table);

        Assert.Equal(["id", "name"], model.Columns.Select(c => c.Name));
        Assert.True(model.Columns[0].Numeric);
        Assert.False(model.Columns[1].Numeric);
    }

    [Fact]
    public async Task Build_NullIsDistinctFromEmptyString()
    {
        var table = new DataTable();
        table.Columns.Add("a", typeof(string));
        table.Columns.Add("b", typeof(string));
        table.Rows.Add(DBNull.Value, string.Empty);

        var model = await BuildFrom(table);
        var row = model.Rows.Single();

        Assert.Null(row[0]);
        Assert.Equal(string.Empty, row[1]);
    }

    [Fact]
    public async Task Build_PassesThroughScalarValues()
    {
        var table = new DataTable();
        table.Columns.Add("n", typeof(int));
        table.Columns.Add("s", typeof(string));
        table.Rows.Add(42, "hello");

        var row = (await BuildFrom(table)).Rows.Single();

        Assert.Equal(42, row[0]);
        Assert.Equal("hello", row[1]);
    }

    [Fact]
    public async Task Build_MaterializesAllRows()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        for (var i = 0; i < 250; i++)
        {
            table.Rows.Add(i);
        }

        var model = await BuildFrom(table);

        Assert.Equal(250, model.Rows.Count);
    }
}
