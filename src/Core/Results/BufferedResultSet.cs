using System.Runtime.CompilerServices;
using Lexql.Core.Abstractions;

namespace Lexql.Core.Results;

public sealed class BufferedResultSet : IResultSet
{
    private readonly IReadOnlyList<IRecord> _records;

    public BufferedResultSet(IReadOnlyList<FieldDescriptor> fields, IReadOnlyList<IRecord> records)
    {
        Fields = fields;
        _records = records;
    }

    public IReadOnlyList<FieldDescriptor> Fields { get; }

    public async IAsyncEnumerable<IRecord> ReadAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        foreach (var record in _records)
        {
            ct.ThrowIfCancellationRequested();
            yield return record;
        }
    }
}
