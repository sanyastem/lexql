namespace Lexql.Core.Relational.Dml;

public enum RowChangeKind
{
    Insert,
    Update,
    Delete,
}

public sealed record RowChange(
    RowChangeKind Kind,
    string Table,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyDictionary<string, object?> Key,
    string? Schema = null);
