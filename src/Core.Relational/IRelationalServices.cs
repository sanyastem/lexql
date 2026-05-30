// Lexql — Core.Relational (OPTIONAL capability services)
// These are obtained via IDatabaseConnection.GetService<T>() and are non-null ONLY
// when the matching ProviderCapabilities flag is set. MySQL/SQLite implement them;
// MongoDB returns null → the UI hides the corresponding features.

#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational;

// Cap: RelationalSchema
public interface IRelationalSchemaReader
{
    Task<RelationalSchema> LoadAsync(string @namespace, CancellationToken ct);
}

// Cap: SqlCompletion — the editor asks Core for suggestions (T08–T10)
public interface ISqlCompletionProvider
{
    Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        string text, int caretOffset, CancellationToken ct);
}

// Cap: ExplainPlan (T13–T14)
public interface IExplainProvider
{
    Task<ExplainPlan> ExplainAsync(string sql, CancellationToken ct);
}

// Cap: SchemaCompare (T17–T19)
public interface ISchemaComparer
{
    SchemaDiff Diff(RelationalSchema source, RelationalSchema target);
}

// Cap: Ddl + SchemaCompare — generate ordered sync script (T18)
public interface IScriptGenerator
{
    string GenerateSyncScript(SchemaDiff diff);
}

// Cap: DataGeneration (T15–T16)
public interface IDataGenerator
{
    Task GenerateAsync(string @namespace, DataGenPlan plan, CancellationToken ct);
}

// ── Supporting models (sketch — fleshed out in their tasks) ──────────────────
public sealed record RelationalSchema(
    string Name,
    IReadOnlyList<TableInfo> Tables,
    IReadOnlyList<ViewInfo> Views,
    IReadOnlyList<RoutineInfo> Routines);

public sealed record TableInfo(
    string Name,
    IReadOnlyList<ColumnInfo> Columns,
    IReadOnlyList<IndexInfo> Indexes,
    IReadOnlyList<ForeignKeyInfo> ForeignKeys,
    IReadOnlyList<string> PrimaryKey,
    IReadOnlyList<CheckConstraintInfo> Checks);

public sealed record ColumnInfo(
    string Name,
    string DataType,
    bool Nullable,
    string? Default,
    bool IsGenerated = false,
    string? GenerationExpression = null);

public sealed record IndexInfo(string Name, IReadOnlyList<string> Columns, bool Unique);
public sealed record ForeignKeyInfo(string Name, IReadOnlyList<string> Columns, string RefTable, IReadOnlyList<string> RefColumns);
public sealed record CheckConstraintInfo(string Name, string Expression);
public sealed record ViewInfo(string Name, string Definition);
public sealed record RoutineInfo(string Name, RoutineKind Kind, string Definition);
public enum RoutineKind { Procedure, Function, Trigger }

public sealed record CompletionItem(string Label, CompletionKind Kind, string? Detail = null);
public enum CompletionKind { Keyword, Table, Column, Function, Schema, Alias }

public sealed record ExplainPlan(ExplainNode Root, string? RawJson);
public sealed record ExplainNode(string Operation, double? EstimatedRows, double? Cost, IReadOnlyList<ExplainNode> Children, IReadOnlyList<string> Warnings);

public sealed record SchemaDiff(IReadOnlyList<SchemaChange> Changes);
public sealed record SchemaChange(ChangeKind Kind, DatabaseObjectKind Target, string Name, string? Detail);
public enum ChangeKind { Added, Removed, Modified }

public sealed record DataGenPlan(IReadOnlyDictionary<string, int> RowsPerTable);
