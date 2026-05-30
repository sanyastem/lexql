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
        var connection = new MySqlConnection(settings.ToConnectionString());
        try
        {
            await connection.OpenAsync(ct);
            var options = new MySqlQueryExecutorOptions(DefaultRowLimit: profile.DefaultRowLimit);
            return new MySqlDatabaseConnection(this, connection, profile.ReadOnly, options);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
