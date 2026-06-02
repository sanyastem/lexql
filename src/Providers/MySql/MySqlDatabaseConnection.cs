using Lexql.Core.Abstractions;
using Lexql.Core.Relational;
using Lexql.Core.Relational.Schema;
using Lexql.Core.Relational.Sql;
using Lexql.Core.Relational.Tree;
using Lexql.Providers.MySql.Execution;
using Lexql.Providers.MySql.Metadata;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlDatabaseConnection : IDatabaseConnection
{
    private readonly MySqlConnection _connection;
    private readonly IAsyncDisposable? _tunnel;
    private readonly IRelationalSchemaReader _schemaReader;
    private readonly IRelationalCatalog _catalog;
    private readonly ISqlCompletionProvider _completion;
    private readonly IServerInfoProvider _serverInfo;

    private readonly IPagedQueryExecutor _pagedExecutor;

    public MySqlDatabaseConnection(
        IDatabaseProvider provider,
        MySqlConnection connection,
        string connectionString,
        bool isReadOnly,
        MySqlQueryExecutorOptions options,
        IAsyncDisposable? tunnel = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(options);

        Provider = provider;
        _connection = connection;
        _tunnel = tunnel;
        IsReadOnly = isReadOnly;

        _schemaReader = new CachingSchemaReader(new MySqlSchemaReader(connection));
        _catalog = new MySqlCatalog(connection);
        _completion = new RelationalCompletionProvider(_schemaReader, connection.Database);
        _serverInfo = new MySqlServerInfo(connection);
        ObjectExplorer = new RelationalObjectExplorer(_catalog, _schemaReader);
        QueryExecutor = new MySqlQueryExecutor(connection, options);
        _pagedExecutor = new MySqlPagedQueryExecutor(connectionString, options);
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
        ?? _pagedExecutor as TService
        ?? _schemaReader as TService
        ?? _catalog as TService
        ?? _completion as TService
        ?? _serverInfo as TService;

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (_tunnel is not null)
        {
            await _tunnel.DisposeAsync();
        }
    }
}
