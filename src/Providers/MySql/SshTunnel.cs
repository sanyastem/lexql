using Lexql.Core.Abstractions;
using Renci.SshNet;

namespace Lexql.Providers.MySql;

public sealed record SshTunnelSettings(
    string Host,
    int Port,
    string User,
    string? Password,
    string? PrivateKeyFile,
    string? PrivateKeyPassphrase)
{
    public const string HostKey = "sshHost";
    public const string PortKey = "sshPort";
    public const string UserKey = "sshUser";
    public const string PasswordKey = "sshPassword";
    public const string KeyFileKey = "sshKeyFile";
    public const string KeyPassphraseKey = "sshKeyPassphrase";

    public const int DefaultPort = 22;

    public static SshTunnelSettings? FromProfile(ConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var settings = profile.Settings;

        var host = Read(settings, HostKey);
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var port = DefaultPort;
        var rawPort = Read(settings, PortKey);
        if (!string.IsNullOrWhiteSpace(rawPort) && int.TryParse(rawPort, out var parsed))
        {
            port = parsed;
        }

        return new SshTunnelSettings(
            host,
            port,
            Read(settings, UserKey) ?? string.Empty,
            Read(settings, PasswordKey),
            Read(settings, KeyFileKey),
            Read(settings, KeyPassphraseKey));
    }

    private static string? Read(IReadOnlyDictionary<string, string?> settings, string key) =>
        settings.TryGetValue(key, out var value) ? value : null;
}

public sealed class SshTunnel : IAsyncDisposable
{
    private readonly SshClient _client;
    private readonly ForwardedPortLocal _port;

    private SshTunnel(SshClient client, ForwardedPortLocal port)
    {
        _client = client;
        _port = port;
    }

    public string LocalHost => "127.0.0.1";

    public int LocalPort => (int)_port.BoundPort;

    public static async Task<SshTunnel> OpenAsync(
        SshTunnelSettings settings, string targetHost, int targetPort, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var client = new SshClient(BuildConnectionInfo(settings));
        try
        {
            await Task.Run(() => client.Connect(), ct);
            var port = new ForwardedPortLocal("127.0.0.1", 0, targetHost, (uint)targetPort);
            client.AddForwardedPort(port);
            port.Start();
            return new SshTunnel(client, port);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static ConnectionInfo BuildConnectionInfo(SshTunnelSettings settings)
    {
        AuthenticationMethod auth;
        if (!string.IsNullOrWhiteSpace(settings.PrivateKeyFile))
        {
            var key = string.IsNullOrEmpty(settings.PrivateKeyPassphrase)
                ? new PrivateKeyFile(settings.PrivateKeyFile)
                : new PrivateKeyFile(settings.PrivateKeyFile, settings.PrivateKeyPassphrase);
            auth = new PrivateKeyAuthenticationMethod(settings.User, key);
        }
        else
        {
            auth = new PasswordAuthenticationMethod(settings.User, settings.Password ?? string.Empty);
        }

        return new ConnectionInfo(settings.Host, settings.Port, settings.User, auth);
    }

    public ValueTask DisposeAsync()
    {
        if (_port.IsStarted)
        {
            _port.Stop();
        }

        _port.Dispose();
        if (_client.IsConnected)
        {
            _client.Disconnect();
        }

        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
