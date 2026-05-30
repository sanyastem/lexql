using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public interface IConnectionOpener
{
    string ProviderId { get; }

    Task<ILiveConnection> OpenAsync(ConnectionProfile profile, CancellationToken ct);

    Task<ConnectionProbe> TestAsync(ConnectionProfile profile, CancellationToken ct);
}
