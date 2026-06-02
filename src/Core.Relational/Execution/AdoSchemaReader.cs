using System.Data.Common;
using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Execution;

public sealed record AdoResultSchema(
    FieldDescriptor[] Fields,
    CellKind[] Kinds,
    Dictionary<string, int> NameIndex);

public static class AdoSchemaReader
{
    public static AdoResultSchema Read(DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

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

        return new AdoResultSchema(fields, kinds, nameIndex);
    }

    public static CellValue[] ReadRow(DbDataReader reader, AdoResultSchema schema)
    {
        var values = new CellValue[schema.Fields.Length];
        for (var i = 0; i < schema.Fields.Length; i++)
        {
            var raw = reader.IsDBNull(i) ? null : reader.GetValue(i);
            values[i] = AdoCellMapper.MapValue(raw, schema.Kinds[i]);
        }

        return values;
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
