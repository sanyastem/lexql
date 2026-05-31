using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Sql;
using Lexql.Providers.MySql.Execution;
using MySqlConnector;

namespace Lexql.Core.Tests.Providers;

public class MySqlReadOnlyExecutorTests
{
    private static MySqlQueryExecutor ReadOnlyExecutor() =>
        new(new MySqlConnection("Server=localhost;User Id=root"), new MySqlQueryExecutorOptions(ReadOnly: true));

    [Fact]
    public async Task ReadOnly_BlocksWriteBeforeConnecting()
    {
        await Assert.ThrowsAsync<ReadOnlyViolationException>(
            () => ReadOnlyExecutor().ExecuteAsync(
                new QueryRequest("UPDATE users SET email = 'x'"), CancellationToken.None));
    }

    [Fact]
    public async Task ReadOnly_BlocksDdl()
    {
        await Assert.ThrowsAsync<ReadOnlyViolationException>(
            () => ReadOnlyExecutor().ExecuteAsync(
                new QueryRequest("DROP TABLE users"), CancellationToken.None));
    }

    [Fact]
    public void Tag_PrependsTraceMarker()
    {
        var tagged = MySqlQueryExecutor.Tag("SELECT 1");

        Assert.StartsWith(MySqlQueryExecutor.TraceMarker, tagged);
        Assert.EndsWith("SELECT 1", tagged);
    }

    [Fact]
    public void Tag_DoesNotChangeStatementClassification()
    {
        Assert.True(SqlStatementClassifier.IsAllowedInReadOnly(MySqlQueryExecutor.Tag("SELECT 1")));
    }
}
