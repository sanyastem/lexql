using Lexql.Core.Connections;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlLiveConnection : ILiveConnection
{
    private readonly MySqlConnection _connection;

    internal MySqlLiveConnection(MySqlConnection connection, ServerVersionInfo server)
    {
        _connection = connection;
        Server = server;
    }

    public string ProviderId => MySqlConnectionOpener.Id;

    public ServerVersionInfo Server { get; }

    public MySqlServerFamily Family => MySqlServerVersion.Classify(Server.Parsed);

    internal MySqlConnection Connection => _connection;

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
