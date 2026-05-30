using Lexql.Core.Abstractions;
using ConnectionProfile = Lexql.Core.Abstractions.ConnectionProfile;

namespace Lexql.App.Services;

public sealed class ConnectionSession
{
    private readonly IDatabaseProvider _provider;

    public ConnectionSession(IDatabaseProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public IDatabaseConnection? Current { get; private set; }

    public bool IsConnected => Current is not null;

    public string? Error { get; private set; }

    public string? ConnectedTo { get; private set; }

    public IReadOnlyList<ConnectionField> Fields => _provider.DescribeConnectionFields();

    public async Task<bool> ConnectAsync(IReadOnlyDictionary<string, string?> settings, CancellationToken ct)
    {
        await DisconnectAsync();
        Error = null;

        try
        {
            var profile = new ConnectionProfile(_provider.Id, "session", settings);
            Current = await _provider.ConnectAsync(profile, ct);
            ConnectedTo = settings.TryGetValue("host", out var host) && !string.IsNullOrWhiteSpace(host)
                ? host
                : _provider.DisplayName;
            return true;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (Current is not null)
        {
            await Current.DisposeAsync();
            Current = null;
            ConnectedTo = null;
        }
    }
}
