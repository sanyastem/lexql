using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly IReadOnlyDictionary<string, IConnectionOpener> _openers;

    public ConnectionManager(IEnumerable<IConnectionOpener> openers)
    {
        ArgumentNullException.ThrowIfNull(openers);

        var map = new Dictionary<string, IConnectionOpener>(StringComparer.OrdinalIgnoreCase);
        foreach (var opener in openers)
        {
            if (!map.TryAdd(opener.ProviderId, opener))
            {
                throw new InvalidOperationException(
                    $"Duplicate connection opener registered for provider '{opener.ProviderId}'.");
            }
        }

        _openers = map;
    }

    public Task<ILiveConnection> OpenAsync(ConnectionProfile profile, CancellationToken ct) =>
        Resolve(profile).OpenAsync(profile, ct);

    public Task<ConnectionProbe> TestAsync(ConnectionProfile profile, CancellationToken ct) =>
        Resolve(profile).TestAsync(profile, ct);

    private IConnectionOpener Resolve(ConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return _openers.TryGetValue(profile.ProviderId, out var opener)
            ? opener
            : throw new UnknownProviderException(profile.ProviderId);
    }
}
