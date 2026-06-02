using System.Data.Common;
using Lexql.Core.Abstractions;
using Lexql.Core.Results;

namespace Lexql.Core.Relational.Execution;

public sealed class AdoQueryCursor : IQueryCursor
{
    private readonly DbDataReader _reader;
    private readonly IAsyncDisposable _owner;
    private readonly AdoResultSchema _schema;
    private IRecord? _pending;
    private bool _exhausted;

    public AdoQueryCursor(
        DbDataReader reader,
        IAsyncDisposable owner,
        long? affectedCount,
        IReadOnlyList<DiagnosticMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(owner);
        _reader = reader;
        _owner = owner;
        _schema = AdoSchemaReader.Read(reader);
        AffectedCount = affectedCount;
        Messages = messages;
        _exhausted = _schema.Fields.Length == 0;
    }

    public IReadOnlyList<FieldDescriptor> Fields => _schema.Fields;

    public long? AffectedCount { get; }

    public IReadOnlyList<DiagnosticMessage> Messages { get; }

    public async Task<ResultPage> FetchAsync(int maxRows, CancellationToken ct)
    {
        if (maxRows < 1)
        {
            maxRows = 1;
        }

        var records = new List<IRecord>(Math.Min(maxRows, 1024));

        if (_pending is not null)
        {
            records.Add(_pending);
            _pending = null;
        }

        while (records.Count < maxRows && !_exhausted && await _reader.ReadAsync(ct))
        {
            records.Add(ReadRecord());
        }

        bool hasMore;
        if (_exhausted || records.Count < maxRows)
        {
            _exhausted = true;
            hasMore = false;
        }
        else if (await _reader.ReadAsync(ct))
        {
            _pending = ReadRecord();
            hasMore = true;
        }
        else
        {
            _exhausted = true;
            hasMore = false;
        }

        return new ResultPage(records, hasMore);
    }

    private IRecord ReadRecord() =>
        new BufferedRecord(_schema.Fields, AdoSchemaReader.ReadRow(_reader, _schema), _schema.NameIndex);

    public async ValueTask DisposeAsync()
    {
        await _reader.DisposeAsync();
        await _owner.DisposeAsync();
    }
}
