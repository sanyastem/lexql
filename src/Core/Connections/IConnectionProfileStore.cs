using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public interface IConnectionProfileStore
{
    Task<IReadOnlyList<ConnectionProfile>> LoadAllAsync(CancellationToken ct);

    Task SaveAllAsync(IReadOnlyList<ConnectionProfile> profiles, CancellationToken ct);
}
