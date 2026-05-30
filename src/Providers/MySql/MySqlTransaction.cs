using Lexql.Core.Abstractions;
using MySqlConnector;

namespace Lexql.Providers.MySql;

internal sealed class MySqlTransactionWrapper : ITransaction
{
    private readonly MySqlTransaction _transaction;

    public MySqlTransactionWrapper(MySqlTransaction transaction) => _transaction = transaction;

    public Task CommitAsync(CancellationToken ct) => _transaction.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct) => _transaction.RollbackAsync(ct);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
