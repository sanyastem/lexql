using Lexql.Core.Abstractions;

namespace Lexql.Core.Results;

public static class ResultGridBuilder
{
    public static async Task<ResultGridModel> BuildAsync(IResultSet resultSet, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(resultSet);

        var columns = resultSet.Fields.Select(ToColumn).ToList();
        var rows = new List<object?[]>();

        await foreach (var record in resultSet.ReadAsync(ct))
        {
            var cells = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                cells[i] = ToCell(record[i]);
            }

            rows.Add(cells);
        }

        return new ResultGridModel(columns, rows);
    }

    public static List<ResultGridColumn> BuildColumns(IReadOnlyList<FieldDescriptor> fields) =>
        fields.Select(ToColumn).ToList();

    public static object?[] RowOf(IRecord record, int columnCount)
    {
        var cells = new object?[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            cells[i] = ToCell(record[i]);
        }

        return cells;
    }

    private static ResultGridColumn ToColumn(FieldDescriptor field) =>
        new(field.Name, field.NativeType ?? field.Kind.ToString(), IsNumeric(field.Kind));

    private static bool IsNumeric(CellKind kind) =>
        kind is CellKind.Integer or CellKind.Float or CellKind.Decimal;

    private static object? ToCell(CellValue value) => value switch
    {
        CellValue.Scalar { Kind: CellKind.Null } => null,
        CellValue.Scalar scalar => scalar.Value,
        CellValue.Document => "{…}",
        CellValue.Array => "[…]",
        _ => null,
    };
}
