using Lexql.Core.Abstractions;

namespace Lexql.Core.Results;

public sealed class BufferedRecord : IRecord
{
    private readonly IReadOnlyList<CellValue> _values;
    private readonly IReadOnlyDictionary<string, int> _nameIndex;

    public BufferedRecord(
        IReadOnlyList<FieldDescriptor> fields,
        IReadOnlyList<CellValue> values,
        IReadOnlyDictionary<string, int> nameIndex)
    {
        Fields = fields;
        _values = values;
        _nameIndex = nameIndex;
    }

    public IReadOnlyList<FieldDescriptor> Fields { get; }

    public CellValue this[int index] => _values[index];

    public CellValue this[string name] =>
        _nameIndex.TryGetValue(name, out var index)
            ? _values[index]
            : throw new KeyNotFoundException($"No field named '{name}'.");
}
