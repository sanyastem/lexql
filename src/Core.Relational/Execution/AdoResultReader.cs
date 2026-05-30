using System.Data.Common;
using Lexql.Core.Abstractions;
using Lexql.Core.Results;

namespace Lexql.Core.Relational.Execution;

public sealed record AdoQueryResult(IReadOnlyList<IResultSet> ResultSets, int RecordsAffected);

public static class AdoResultReader
{
    public static async Task<AdoQueryResult> ReadAllAsync(DbDataReader reader, int? rowLimit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var resultSets = new List<IResultSet>();
        do
        {
            if (reader.FieldCount > 0)
            {
                resultSets.Add(await ReadResultSetAsync(reader, rowLimit, ct));
            }
        }
        while (await reader.NextResultAsync(ct));

        return new AdoQueryResult(resultSets, reader.RecordsAffected);
    }

    private static async Task<IResultSet> ReadResultSetAsync(DbDataReader reader, int? rowLimit, CancellationToken ct)
    {
        var fieldCount = reader.FieldCount;
        var fields = new FieldDescriptor[fieldCount];
        var kinds = new CellKind[fieldCount];
        var nameIndex = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < fieldCount; i++)
        {
            var kind = AdoCellMapper.MapKind(reader.GetFieldType(i));
            var name = reader.GetName(i);
            kinds[i] = kind;
            fields[i] = new FieldDescriptor(name, kind, SafeTypeName(reader, i));
            nameIndex[name] = i;
        }

        var records = new List<IRecord>();
        while ((rowLimit is null || records.Count < rowLimit) && await reader.ReadAsync(ct))
        {
            var values = new CellValue[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                var raw = reader.IsDBNull(i) ? null : reader.GetValue(i);
                values[i] = AdoCellMapper.MapValue(raw, kinds[i]);
            }

            records.Add(new BufferedRecord(fields, values, nameIndex));
        }

        return new BufferedResultSet(fields, records);
    }

    private static string? SafeTypeName(DbDataReader reader, int ordinal)
    {
        try
        {
            return reader.GetDataTypeName(ordinal);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }
}
