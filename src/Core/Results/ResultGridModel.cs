namespace Lexql.Core.Results;

public sealed record ResultGridColumn(string Name, string Type, bool Numeric);

public sealed record ResultGridModel(
    IReadOnlyList<ResultGridColumn> Columns,
    IReadOnlyList<object?[]> Rows);
