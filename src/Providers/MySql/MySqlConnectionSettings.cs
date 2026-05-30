using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using MySqlConnector;

namespace Lexql.Providers.MySql;

public sealed class MySqlConnectionSettings
{
    public const string HostKey = "host";
    public const string PortKey = "port";
    public const string UserKey = "user";
    public const string PasswordKey = "password";
    public const string DatabaseKey = "database";
    public const string SslModeKey = "sslMode";

    public const int DefaultPort = 3306;

    public required string Host { get; init; }

    public int Port { get; init; } = DefaultPort;

    public required string User { get; init; }

    public string? Password { get; init; }

    public string? Database { get; init; }

    public MySqlSslMode SslMode { get; init; } = MySqlSslMode.Preferred;

    public static MySqlConnectionSettings FromProfile(ConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var settings = profile.Settings;
        var errors = new List<string>();

        var host = Read(settings, HostKey);
        if (string.IsNullOrWhiteSpace(host))
        {
            errors.Add("Host is required.");
        }

        var user = Read(settings, UserKey);
        if (string.IsNullOrWhiteSpace(user))
        {
            errors.Add("User is required.");
        }

        var port = DefaultPort;
        var rawPort = Read(settings, PortKey);
        if (!string.IsNullOrWhiteSpace(rawPort) &&
            (!int.TryParse(rawPort, out port) || port is < 1 or > 65535))
        {
            errors.Add($"Port '{rawPort}' is not a valid TCP port.");
        }

        var sslMode = MySqlSslMode.Preferred;
        var rawSsl = Read(settings, SslModeKey);
        if (!string.IsNullOrWhiteSpace(rawSsl) && !Enum.TryParse(rawSsl, ignoreCase: true, out sslMode))
        {
            errors.Add($"SSL mode '{rawSsl}' is not recognized.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidConnectionProfileException(profile.Name, errors);
        }

        return new MySqlConnectionSettings
        {
            Host = host!,
            Port = port,
            User = user!,
            Password = Read(settings, PasswordKey),
            Database = Read(settings, DatabaseKey),
            SslMode = sslMode,
        };
    }

    public string ToConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Host,
            Port = (uint)Port,
            UserID = User,
            SslMode = SslMode,
        };

        if (Password is not null)
        {
            builder.Password = Password;
        }

        if (!string.IsNullOrWhiteSpace(Database))
        {
            builder.Database = Database;
        }

        return builder.ConnectionString;
    }

    private static string? Read(IReadOnlyDictionary<string, string?> settings, string key) =>
        settings.TryGetValue(key, out var value) ? value : null;
}
