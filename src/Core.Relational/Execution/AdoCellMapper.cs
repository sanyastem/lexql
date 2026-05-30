using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Execution;

public static class AdoCellMapper
{
    public static CellKind MapKind(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(bool))
        {
            return CellKind.Boolean;
        }

        if (t == typeof(sbyte) || t == typeof(byte) || t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong))
        {
            return CellKind.Integer;
        }

        if (t == typeof(float) || t == typeof(double))
        {
            return CellKind.Float;
        }

        if (t == typeof(decimal))
        {
            return CellKind.Decimal;
        }

        if (t == typeof(byte[]))
        {
            return CellKind.Bytes;
        }

        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
        {
            return CellKind.DateTime;
        }

        return CellKind.String;
    }

    public static CellValue MapValue(object? raw, CellKind kind) =>
        raw is null or DBNull
            ? new CellValue.Scalar(CellKind.Null, null)
            : new CellValue.Scalar(kind, raw);
}
