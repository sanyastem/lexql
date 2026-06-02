using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Execution;
using Microsoft.Data.Sqlite;

namespace Lexql.Core.Tests.Execution;

public class AdoQueryCursorTests
{
    [Fact]
    public async Task Fetch_PagesThroughAllRows_WithRemainder()
    {
        await using var cursor = await OpenCursorAsync(25);

        Assert.Equal(2, cursor.Fields.Count);
        Assert.Null(cursor.AffectedCount);

        var first = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Equal(10, first.Records.Count);
        Assert.True(first.HasMore);

        var second = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Equal(10, second.Records.Count);
        Assert.True(second.HasMore);

        var third = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Equal(5, third.Records.Count);
        Assert.False(third.HasMore);
    }

    [Fact]
    public async Task Fetch_ExactMultiple_LastFullPageReportsNoMore()
    {
        await using var cursor = await OpenCursorAsync(20);

        var first = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Equal(10, first.Records.Count);
        Assert.True(first.HasMore);

        var second = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Equal(10, second.Records.Count);
        Assert.False(second.HasMore);
    }

    [Fact]
    public async Task Fetch_EmptyResult_ReturnsNoRowsAndNoMore()
    {
        await using var cursor = await OpenCursorAsync(0);

        var page = await cursor.FetchAsync(10, CancellationToken.None);
        Assert.Empty(page.Records);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Fetch_PreservesRowOrderAcrossPages()
    {
        await using var cursor = await OpenCursorAsync(15);

        var seen = new List<long>();
        ResultPage page;
        do
        {
            page = await cursor.FetchAsync(4, CancellationToken.None);
            foreach (var record in page.Records)
            {
                seen.Add((long)((CellValue.Scalar)record["id"]).Value!);
            }
        }
        while (page.HasMore);

        Assert.Equal(Enumerable.Range(1, 15).Select(i => (long)i), seen);
    }

    private static async Task<AdoQueryCursor> OpenCursorAsync(int rows)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE t(id INTEGER, name TEXT);";
            await create.ExecuteNonQueryAsync();
        }

        if (rows > 0)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText =
                "WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x + 1 FROM seq WHERE x < $n) " +
                "INSERT INTO t(id, name) SELECT x, 'n' || x FROM seq;";
            insert.Parameters.AddWithValue("$n", rows);
            await insert.ExecuteNonQueryAsync();
        }

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM t ORDER BY id";
        var reader = await command.ExecuteReaderAsync();
        var owner = new CommandOwner(connection, command);
        return new AdoQueryCursor(reader, owner, affectedCount: null, messages: []);
    }

    private sealed class CommandOwner : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SqliteCommand _command;

        public CommandOwner(SqliteConnection connection, SqliteCommand command)
        {
            _connection = connection;
            _command = command;
        }

        public async ValueTask DisposeAsync()
        {
            await _command.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
