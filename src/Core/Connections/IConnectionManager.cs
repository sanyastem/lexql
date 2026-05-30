using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public interface IConnectionManager
{
    Task<ILiveConnection> OpenAsync(ConnectionProfile profile, CancellationToken ct);

    Task<ConnectionProbe> TestAsync(ConnectionProfile profile, CancellationToken ct);
}
