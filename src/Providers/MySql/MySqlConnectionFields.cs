using Lexql.Core.Abstractions;

namespace Lexql.Providers.MySql;

public static class MySqlConnectionFields
{
    public static IReadOnlyList<ConnectionField> Describe() =>
    [
        new(MySqlConnectionSettings.HostKey, "Host", ConnectionFieldKind.Text, Required: true, Default: "127.0.0.1"),
        new(MySqlConnectionSettings.PortKey, "Port", ConnectionFieldKind.Number, Required: false, Default: "3306"),
        new(MySqlConnectionSettings.UserKey, "User", ConnectionFieldKind.Text, Required: true, Default: "root"),
        new(MySqlConnectionSettings.PasswordKey, "Password", ConnectionFieldKind.Password, Required: false),
        new(MySqlConnectionSettings.DatabaseKey, "Database", ConnectionFieldKind.Text, Required: false),
        new(MySqlConnectionSettings.SslModeKey, "SSL Mode", ConnectionFieldKind.Choice, Required: false, Default: "Preferred"),
        new(MySqlConnectionSettings.SslCaKey, "SSL CA cert", ConnectionFieldKind.FilePath, Required: false),
        new(SshTunnelSettings.HostKey, "SSH host", ConnectionFieldKind.Text, Required: false),
        new(SshTunnelSettings.PortKey, "SSH port", ConnectionFieldKind.Number, Required: false, Default: "22"),
        new(SshTunnelSettings.UserKey, "SSH user", ConnectionFieldKind.Text, Required: false),
        new(SshTunnelSettings.PasswordKey, "SSH password", ConnectionFieldKind.Password, Required: false),
        new(SshTunnelSettings.KeyFileKey, "SSH key file", ConnectionFieldKind.FilePath, Required: false),
    ];
}
