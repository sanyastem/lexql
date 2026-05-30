using Lexql.Core.Abstractions;
using Lexql.Providers.MySql.Execution;
using MySqlConnector;

namespace Lexql.Core.Tests.Providers;

public class MySqlCancellationTests
{
    [Fact]
    public async Task ExecuteAsync_AlreadyCancelledToken_Throws()
    {
        var executor = new MySqlQueryExecutor(new MySqlConnection("Server=localhost;User Id=root"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => executor.ExecuteAsync(new QueryRequest("SELECT 1"), cts.Token));
    }
}
