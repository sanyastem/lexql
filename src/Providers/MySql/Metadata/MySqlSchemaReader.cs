using System.Data;
using System.Data.Common;
using Lexql.Core.Relational;
using MySqlConnector;

namespace Lexql.Providers.MySql.Metadata;

public sealed class MySqlSchemaReader : IRelationalSchemaReader
{
    private readonly MySqlConnection _connection;

    public MySqlSchemaReader(MySqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public async Task<RelationalSchema> LoadAsync(string @namespace, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        var serverVersion = MySqlServerVersion.Parse(_connection.ServerVersion);

        var tables = await QueryAsync(@namespace, MySqlMetadataQueries.Tables, MapTable, ct);
        var columns = await QueryAsync(@namespace, MySqlMetadataQueries.Columns, MapColumn, ct);
        var primaryKeys = await QueryAsync(@namespace, MySqlMetadataQueries.PrimaryKeys, MapPrimaryKey, ct);
        var indexes = await QueryAsync(@namespace, MySqlMetadataQueries.Indexes, MapIndex, ct);
        var foreignKeys = await QueryAsync(@namespace, MySqlMetadataQueries.ForeignKeys, MapForeignKey, ct);
        var views = await QueryAsync(@namespace, MySqlMetadataQueries.Views, MapView, ct);
        var routines = await QueryAsync(@namespace, MySqlMetadataQueries.Routines, MapRoutine, ct);
        var triggers = await QueryAsync(@namespace, MySqlMetadataQueries.Triggers, MapTrigger, ct);

        var checks = MySqlMetadataQueries.SupportsCheckConstraints(serverVersion)
            ? await QueryAsync(@namespace, MySqlMetadataQueries.Checks, MapCheck, ct)
            : [];

        var rows = new MySqlSchemaRows(
            @namespace, tables, columns, primaryKeys, indexes, foreignKeys, checks, views, routines, triggers);

        return MySqlSchemaMapper.Map(rows);
    }

    private async Task<List<T>> QueryAsync<T>(string schema, string sql, Func<DbDataReader, T> map, CancellationToken ct)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new MySqlParameter("@schema", schema));

        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(map(reader));
        }

        return rows;
    }

    private static MySqlTableRow MapTable(DbDataReader r) => new(r.GetString(0));

    private static MySqlColumnRow MapColumn(DbDataReader r) => new(
        r.GetString(0),
        r.GetString(1),
        r.GetInt32(2),
        r.GetString(3),
        string.Equals(r.GetString(4), "YES", StringComparison.OrdinalIgnoreCase),
        NullableString(r, 5),
        r.GetString(6),
        NullableString(r, 7));

    private static MySqlPrimaryKeyRow MapPrimaryKey(DbDataReader r) => new(
        r.GetString(0), r.GetString(1), r.GetInt32(2));

    private static MySqlIndexRow MapIndex(DbDataReader r) => new(
        r.GetString(0),
        r.GetString(1),
        r.GetString(2),
        r.GetInt32(3),
        r.GetInt32(4) != 0);

    private static MySqlForeignKeyRow MapForeignKey(DbDataReader r) => new(
        r.GetString(0),
        r.GetString(1),
        r.GetString(2),
        r.GetInt32(3),
        r.GetString(4),
        r.GetString(5));

    private static MySqlCheckRow MapCheck(DbDataReader r) => new(
        r.GetString(0), r.GetString(1), r.GetString(2));

    private static MySqlViewRow MapView(DbDataReader r) => new(
        r.GetString(0), NullableString(r, 1) ?? string.Empty);

    private static MySqlRoutineRow MapRoutine(DbDataReader r) => new(
        r.GetString(0), r.GetString(1), NullableString(r, 2) ?? string.Empty);

    private static MySqlTriggerRow MapTrigger(DbDataReader r) => new(
        r.GetString(0), NullableString(r, 1) ?? string.Empty);

    private static string? NullableString(DbDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? null : r.GetString(ordinal);
}
