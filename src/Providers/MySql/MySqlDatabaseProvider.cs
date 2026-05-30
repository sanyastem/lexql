using Lexql.Core.Abstractions;
using Lexql.Providers.MySql.Execution;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlDatabaseProvider : IDatabaseProvider
{
    public string Id => MySqlConnectionOpener.Id;

    public string DisplayName => "MySQL";

    public ProviderCapabilities Capabilities =>
        ProviderCapabilities.QueryText
        | ProviderCapabilities.ObjectExplorer
        | ProviderCapabilities.Transactions
        | ProviderCapabilities.RelationalSchema
        | ProviderCapabilities.SqlCompletion;

    public IReadOnlyList<ConnectionField> DescribeConnectionFields() => MySqlConnectionFields.Describe();

    public async Task<IDatabaseConnection> ConnectAsync(ConnectionProfile profile, CancellationToken ct)
    {
        var settings = MySqlConnectionSettings.FromProfile(profile);
        var sshSettings = SshTunnelSettings.FromProfile(profile);

        SshTunnel? tunnel = null;
        if (sshSettings is not null)
        {
            tunnel = await SshTunnel.OpenAsync(sshSettings, settings.Host, settings.Port, ct);
        }

        var connection = new MySqlConnection(BuildConnectionString(settings, tunnel));
        try
        {
            await connection.OpenAsync(ct);
            var options = new MySqlQueryExecutorOptions(
                DefaultRowLimit: profile.DefaultRowLimit, ReadOnly: profile.ReadOnly);
            return new MySqlDatabaseConnection(this, connection, profile.ReadOnly, options, tunnel);
        }
        catch
        {
            await connection.DisposeAsync();
            if (tunnel is not null)
            {
                await tunnel.DisposeAsync();
            }

            throw;
        }
    }

    private static string BuildConnectionString(MySqlConnectionSettings settings, SshTunnel? tunnel)
    {
        if (tunnel is null)
        {
            return settings.ToConnectionString();
        }

        return new MySqlConnectionStringBuilder(settings.ToConnectionString())
        {
            Server = tunnel.LocalHost,
            Port = (uint)tunnel.LocalPort,
        }.ConnectionString;
    }
}
