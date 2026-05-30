using System.Diagnostics;
using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlConnectionOpener : IConnectionOpener
{
    public const string Id = "mysql";

    public string ProviderId => Id;

    public async Task<ILiveConnection> OpenAsync(ConnectionProfile profile, CancellationToken ct)
    {
        var settings = MySqlConnectionSettings.FromProfile(profile);
        var connection = new MySqlConnection(settings.ToConnectionString());
        try
        {
            await connection.OpenAsync(ct);
            return new MySqlLiveConnection(connection, ReadServerVersion(connection));
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<ConnectionProbe> TestAsync(ConnectionProfile profile, CancellationToken ct)
    {
        var settings = MySqlConnectionSettings.FromProfile(profile);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new MySqlConnection(settings.ToConnectionString());
            await connection.OpenAsync(ct);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(ct);

            stopwatch.Stop();
            return ConnectionProbe.Ok(ReadServerVersion(connection), stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return ConnectionProbe.Failed(ex.Message, stopwatch.Elapsed);
        }
    }

    private static ServerVersionInfo ReadServerVersion(MySqlConnection connection)
    {
        var raw = connection.ServerVersion;
        return new ServerVersionInfo(raw, MySqlServerVersion.Parse(raw));
    }
}
