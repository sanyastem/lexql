namespace Lexql.Core.History;

public sealed record QueryHistoryEntry(long Id, string Sql, string? ConnectionName, DateTimeOffset ExecutedAt, bool Success);

public sealed record SavedQuery(long Id, string Name, string Sql, DateTimeOffset SavedAt);

public interface IQueryHistoryStore
{
    Task InitializeAsync(CancellationToken ct);

    Task AddHistoryAsync(string sql, string? connectionName, bool success, CancellationToken ct);

    Task<IReadOnlyList<QueryHistoryEntry>> SearchHistoryAsync(string? text, int limit, CancellationToken ct);

    Task SaveQueryAsync(string name, string sql, CancellationToken ct);

    Task<IReadOnlyList<SavedQuery>> ListSavedAsync(CancellationToken ct);

    Task DeleteSavedAsync(long id, CancellationToken ct);
}
