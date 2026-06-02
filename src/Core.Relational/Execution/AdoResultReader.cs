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
        var schema = AdoSchemaReader.Read(reader);

        var records = new List<IRecord>();
        while ((rowLimit is null || records.Count < rowLimit) && await reader.ReadAsync(ct))
        {
            var values = AdoSchemaReader.ReadRow(reader, schema);
            records.Add(new BufferedRecord(schema.Fields, values, schema.NameIndex));
        }

        return new BufferedResultSet(schema.Fields, records);
    }
}
