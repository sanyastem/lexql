using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Dml;

public static class DmlApplier
{
    public static async Task ApplyAsync(
        IQueryExecutor executor, IReadOnlyList<DmlCommand> commands, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(commands);

        await executor.ExecuteAsync(new QueryRequest("START TRANSACTION"), ct);
        try
        {
            foreach (var command in commands)
            {
                await executor.ExecuteAsync(new QueryRequest(command.Sql, Parameters: command.Parameters), ct);
            }

            await executor.ExecuteAsync(new QueryRequest("COMMIT"), ct);
        }
        catch
        {
            try
            {
                await executor.ExecuteAsync(new QueryRequest("ROLLBACK"), ct);
            }
            catch (Exception)
            {
            }

            throw;
        }
    }
}
