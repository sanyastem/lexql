using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Lexql.Core.History;

public sealed class SqliteQueryHistoryStore : IQueryHistoryStore
{
    private readonly string _connectionString;

    public SqliteQueryHistoryStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await ExecuteAsync(connection,
            """
            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sql TEXT NOT NULL,
                connection TEXT NULL,
                executed_at TEXT NOT NULL,
                success INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS saved (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                sql TEXT NOT NULL,
                saved_at TEXT NOT NULL);
            """,
            ct);
    }

    public async Task AddHistoryAsync(string sql, string? connectionName, bool success, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sql);
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO history (sql, connection, executed_at, success) VALUES ($sql, $conn, $at, $ok)";
        command.Parameters.AddWithValue("$sql", sql);
        command.Parameters.AddWithValue("$conn", (object?)connectionName ?? DBNull.Value);
        command.Parameters.AddWithValue("$at", Now());
        command.Parameters.AddWithValue("$ok", success ? 1 : 0);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<QueryHistoryEntry>> SearchHistoryAsync(string? text, int limit, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, sql, connection, executed_at, success FROM history
            WHERE ($text IS NULL OR sql LIKE $like)
            ORDER BY id DESC LIMIT $limit
            """;
        command.Parameters.AddWithValue("$text", (object?)text ?? DBNull.Value);
        command.Parameters.AddWithValue("$like", $"%{text}%");
        command.Parameters.AddWithValue("$limit", limit);

        var entries = new List<QueryHistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            entries.Add(new QueryHistoryEntry(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                ParseDate(reader.GetString(3)),
                reader.GetInt64(4) != 0));
        }

        return entries;
    }

    public async Task SaveQueryAsync(string name, string sql, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sql);
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO saved (name, sql, saved_at) VALUES ($name, $sql, $at)
            ON CONFLICT(name) DO UPDATE SET sql = $sql, saved_at = $at
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$sql", sql);
        command.Parameters.AddWithValue("$at", Now());
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SavedQuery>> ListSavedAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, sql, saved_at FROM saved ORDER BY name";

        var saved = new List<SavedQuery>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            saved.Add(new SavedQuery(
                reader.GetInt64(0), reader.GetString(1), reader.GetString(2), ParseDate(reader.GetString(3))));
        }

        return saved;
    }

    public async Task DeleteSavedAsync(long id, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM saved WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string Now() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDate(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
