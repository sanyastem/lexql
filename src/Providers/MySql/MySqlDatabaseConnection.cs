using Lexql.Core.Abstractions;
using Lexql.Core.Relational;
using Lexql.Core.Relational.Schema;
using Lexql.Core.Relational.Tree;
using Lexql.Providers.MySql.Execution;
using Lexql.Providers.MySql.Metadata;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlDatabaseConnection : IDatabaseConnection
{
    private readonly MySqlConnection _connection;
    private readonly IRelationalSchemaReader _schemaReader;
    private readonly IRelationalCatalog _catalog;

    public MySqlDatabaseConnection(
        IDatabaseProvider provider,
        MySqlConnection connection,
        bool isReadOnly,
        MySqlQueryExecutorOptions options)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);

        Provider = provider;
        _connection = connection;
        IsReadOnly = isReadOnly;

        _schemaReader = new CachingSchemaReader(new MySqlSchemaReader(connection));
        _catalog = new MySqlCatalog(connection);
        ObjectExplorer = new RelationalObjectExplorer(_catalog, _schemaReader);
        QueryExecutor = new MySqlQueryExecutor(connection, options);
    }

    public IDatabaseProvider Provider { get; }

    public bool IsReadOnly { get; }

    public IObjectExplorer ObjectExplorer { get; }

    public IQueryExecutor QueryExecutor { get; }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct) =>
        new MySqlTransactionWrapper(await _connection.BeginTransactionAsync(ct));

    public TService? GetService<TService>() where TService : class =>
        ObjectExplorer as TService
        ?? QueryExecutor as TService
        ?? _schemaReader as TService
        ?? _catalog as TService;

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
