namespace Lexql.Providers.MySql.Metadata;

public sealed record MySqlTableRow(string TableName);

public sealed record MySqlColumnRow(
    string TableName,
    string ColumnName,
    int OrdinalPosition,
    string ColumnType,
    bool IsNullable,
    string? Default,
    string Extra,
    string? GenerationExpression);

public sealed record MySqlPrimaryKeyRow(string TableName, string ColumnName, int Ordinal);

public sealed record MySqlIndexRow(
    string TableName,
    string IndexName,
    string ColumnName,
    int SequenceInIndex,
    bool NonUnique);

public sealed record MySqlForeignKeyRow(
    string TableName,
    string ConstraintName,
    string ColumnName,
    int Ordinal,
    string ReferencedTable,
    string ReferencedColumn);

public sealed record MySqlCheckRow(string TableName, string ConstraintName, string CheckClause);

public sealed record MySqlViewRow(string ViewName, string Definition);

public sealed record MySqlRoutineRow(string RoutineName, string RoutineType, string Definition);

public sealed record MySqlTriggerRow(string TriggerName, string Statement);

public sealed record MySqlSchemaRows(
    string Schema,
    IReadOnlyList<MySqlTableRow> Tables,
    IReadOnlyList<MySqlColumnRow> Columns,
    IReadOnlyList<MySqlPrimaryKeyRow> PrimaryKeys,
    IReadOnlyList<MySqlIndexRow> Indexes,
    IReadOnlyList<MySqlForeignKeyRow> ForeignKeys,
    IReadOnlyList<MySqlCheckRow> Checks,
    IReadOnlyList<MySqlViewRow> Views,
    IReadOnlyList<MySqlRoutineRow> Routines,
    IReadOnlyList<MySqlTriggerRow> Triggers);
