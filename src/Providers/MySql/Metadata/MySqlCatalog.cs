using System.Data;
using Lexql.Core.Relational.Tree;
using MySqlConnector;

namespace Lexql.Providers.MySql.Metadata;

public sealed class MySqlCatalog : IRelationalCatalog
{
    private static readonly string[] SystemSchemas =
        ["mysql", "information_schema", "performance_schema", "sys"];

    private const string ListSchemas = """
        SELECT SCHEMA_NAME
        FROM information_schema.SCHEMATA
        ORDER BY SCHEMA_NAME;
        """;

    private readonly MySqlConnection _connection;
    private readonly bool _includeSystemSchemas;

    public MySqlCatalog(MySqlConnection connection, bool includeSystemSchemas = false)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _includeSystemSchemas = includeSystemSchemas;
    }

    public async Task<IReadOnlyList<string>> ListNamespacesAsync(CancellationToken ct)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        using var command = _connection.CreateCommand();
        command.CommandText = ListSchemas;

        var schemas = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0);
            if (_includeSystemSchemas || !SystemSchemas.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                schemas.Add(name);
            }
        }

        return schemas;
    }
}
