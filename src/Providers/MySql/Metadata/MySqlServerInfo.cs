using System.Data;
using Lexql.Core.Abstractions;
using MySqlConnector;

namespace Lexql.Providers.MySql.Metadata;

public sealed class MySqlServerInfo : IServerInfoProvider
{
    private const string Query = """
        SELECT
            VERSION()                  AS version,
            @@version_comment          AS edition,
            @@hostname                 AS host,
            @@port                     AS port,
            CURRENT_USER()             AS account,
            @@character_set_server     AS charset,
            @@collation_server         AS collation,
            DATABASE()                 AS db
        """;

    private readonly MySqlConnection _connection;

    public MySqlServerInfo(MySqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public async Task<IReadOnlyList<ServerInfoItem>> GetServerInfoAsync(CancellationToken ct)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        await using var command = _connection.CreateCommand();
        command.CommandText = Query;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return [];
        }

        var items = new List<ServerInfoItem>();
        Add(items, reader, "version", "Version");
        Add(items, reader, "edition", "Edition");
        Add(items, reader, "host", "Host");
        Add(items, reader, "port", "Port");
        Add(items, reader, "account", "User");
        Add(items, reader, "charset", "Server charset");
        Add(items, reader, "collation", "Server collation");
        Add(items, reader, "db", "Current database");
        return items;
    }

    private static void Add(List<ServerInfoItem> items, MySqlDataReader reader, string column, string label)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return;
        }

        var value = reader.GetValue(ordinal)?.ToString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            items.Add(new ServerInfoItem(label, value));
        }
    }
}
