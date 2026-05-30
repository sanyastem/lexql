using System.Collections.Concurrent;

namespace Lexql.Core.Relational.Schema;

public sealed class CachingSchemaReader : IRelationalSchemaReader, ISchemaCache
{
    private readonly IRelationalSchemaReader _inner;
    private readonly ConcurrentDictionary<string, Lazy<Task<RelationalSchema>>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public CachingSchemaReader(IRelationalSchemaReader inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public async Task<RelationalSchema> LoadAsync(string @namespace, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(@namespace);

        var entry = _cache.GetOrAdd(
            @namespace,
            key => new Lazy<Task<RelationalSchema>>(() => _inner.LoadAsync(key, ct)));

        try
        {
            return await entry.Value;
        }
        catch
        {
            _cache.TryRemove(@namespace, out _);
            throw;
        }
    }

    public void Invalidate(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        _cache.TryRemove(@namespace, out _);
    }

    public void InvalidateAll() => _cache.Clear();
}
