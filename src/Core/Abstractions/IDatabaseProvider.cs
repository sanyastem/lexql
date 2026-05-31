// Lexql — Core.Abstractions
// Generic, DB-agnostic provider spine. NOT SQL-shaped (ADR-002).
// Relational features (SQL parsing, DDL, schema compare, EXPLAIN, data gen) are
// OPTIONAL services obtained via GetService<T>() and gated by ProviderCapabilities.
// Validated on paper against MySQL (relational) and MongoDB (document) — see notes at bottom.

#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexql.Core.Abstractions;

// ─────────────────────────────────────────────────────────────────────────────
// Provider
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A database engine plugin (mysql, sqlite, mongodb, ...).</summary>
public interface IDatabaseProvider
{
    /// <summary>Stable id, e.g. "mysql", "sqlite", "mongodb".</summary>
    string Id { get; }

    string DisplayName { get; }

    /// <summary>What this provider can do. Drives UI feature gating.</summary>
    ProviderCapabilities Capabilities { get; }

    /// <summary>Fields the connection form should render (host, port, file, URI, ...).
    /// Lets the UI build the connect dialog generically, per provider.</summary>
    IReadOnlyList<ConnectionField> DescribeConnectionFields();

    Task<IDatabaseConnection> ConnectAsync(ConnectionProfile profile, CancellationToken ct);
}

/// <summary>Capability flags. Contract: if a flag is set, the corresponding
/// optional service (GetService&lt;T&gt;) is guaranteed non-null.</summary>
[System.Flags]
public enum ProviderCapabilities
{
    None           = 0,

    // Universal
    QueryText      = 1 << 0,  // can execute free-form query text (SQL / MQL / ...)
    ObjectExplorer = 1 << 1,
    Transactions   = 1 << 2,
    ReadOnlyMode   = 1 << 3,  // ADR-010
    RowLimit       = 1 << 4,  // ADR-010

    // Relational (Core.Relational services)
    RelationalSchema = 1 << 5, // tables/columns/FK/indexes
    Ddl              = 1 << 6,
    SchemaCompare    = 1 << 7,
    SqlCompletion    = 1 << 8,
    ExplainPlan      = 1 << 9,
    DataGeneration   = 1 << 10,

    // Document (future Mongo services)
    DocumentStore  = 1 << 11,
    Aggregation    = 1 << 12,
}

// ─────────────────────────────────────────────────────────────────────────────
// Connection
// ─────────────────────────────────────────────────────────────────────────────

public sealed record ConnectionProfile(
    string ProviderId,
    string Name,
    IReadOnlyDictionary<string, string?> Settings, // provider-defined keys
    bool ReadOnly = false,                          // ADR-010
    int? DefaultRowLimit = null);                   // ADR-010

public sealed record ConnectionField(
    string Key,
    string Label,
    ConnectionFieldKind Kind,
    bool Required,
    string? Default = null,
    IReadOnlyList<string>? Options = null,
    bool Advanced = false);

public enum ConnectionFieldKind { Text, Password, Number, Boolean, Choice, FilePath }

public interface IDatabaseConnection : IAsyncDisposable
{
    IDatabaseProvider Provider { get; }
    bool IsReadOnly { get; }            // enforced per-provider (see notes)

    IObjectExplorer ObjectExplorer { get; }
    IQueryExecutor QueryExecutor { get; }

    Task<ITransaction> BeginTransactionAsync(CancellationToken ct);

    /// <summary>Optional capability services. Returns null if unsupported.
    /// e.g. GetService&lt;ISqlCompletionProvider&gt;() — null for MongoDB.</summary>
    TService? GetService<TService>() where TService : class;
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}

// ─────────────────────────────────────────────────────────────────────────────
// Object explorer (generic tree)
// ─────────────────────────────────────────────────────────────────────────────

public interface IObjectExplorer
{
    Task<IReadOnlyList<DatabaseObjectNode>> GetRootsAsync(CancellationToken ct);
    Task<IReadOnlyList<DatabaseObjectNode>> GetChildrenAsync(DatabaseObjectNode node, CancellationToken ct);
}

public sealed record DatabaseObjectNode(
    string Id,
    string Name,
    DatabaseObjectKind Kind,
    bool HasChildren);

public enum DatabaseObjectKind
{
    Namespace,   // MySQL schema / Mongo database
    Container,   // MySQL table / Mongo collection
    View,
    Routine,
    Trigger,
    Field,       // column / inferred document field
    Index,
    ForeignKey,
    Other,
}

// ─────────────────────────────────────────────────────────────────────────────
// Query execution (opaque text in, generic records out)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Text is opaque to Core: SQL for relational, MQL/JSON pipeline for Mongo.
/// The provider knows how to run it.</summary>
public sealed record QueryRequest(
    string Text,
    int? RowLimit = null,                                   // ADR-010; provider applies it
    IReadOnlyList<QueryParameter>? Parameters = null);      // honored only if provider supports it

public sealed record QueryParameter(string Name, object? Value);

public interface IQueryExecutor
{
    Task<QueryExecution> ExecuteAsync(QueryRequest request, CancellationToken ct);
}

public sealed class QueryExecution
{
    public required IReadOnlyList<IResultSet> ResultSets { get; init; } // batch → many (relational); usually 1 (Mongo)
    public long? AffectedCount { get; init; }
    public TimeSpan Elapsed { get; init; }
    public IReadOnlyList<DiagnosticMessage> Messages { get; init; } = [];
}

public sealed record DiagnosticMessage(DiagnosticSeverity Severity, string Text);
public enum DiagnosticSeverity { Info, Warning, Error }

// ─────────────────────────────────────────────────────────────────────────────
// Result model — THE crux. Scalar OR nested → works for rows AND documents.
// ─────────────────────────────────────────────────────────────────────────────

public interface IResultSet
{
    /// <summary>Best-known fields. For relational: exact, upfront from metadata.
    /// For documents: inferred (may be empty until records are read).</summary>
    IReadOnlyList<FieldDescriptor> Fields { get; }

    /// <summary>Streamed — supports huge results and Mongo cursors (T24).</summary>
    IAsyncEnumerable<IRecord> ReadAsync(CancellationToken ct);
}

public interface IRecord
{
    /// <summary>Per-record fields. For documents this can differ row-to-row.</summary>
    IReadOnlyList<FieldDescriptor> Fields { get; }
    CellValue this[int index] { get; }
    CellValue this[string name] { get; }
}

public sealed record FieldDescriptor(string Name, CellKind Kind, string? NativeType);

public enum CellKind
{
    Null, Boolean, Integer, Float, Decimal, String, Bytes, DateTime,
    Document, // nested object (Mongo) — degenerate-absent in plain relational
    Array,    // nested array
}

/// <summary>A cell is either a scalar, a nested document, or an array.
/// A relational row = record of Scalar cells. A Mongo document = record that
/// may contain Document/Array cells. This is why the model is not SQL-shaped.</summary>
public abstract record CellValue
{
    public sealed record Scalar(CellKind Kind, object? Value) : CellValue;
    public sealed record Document(IReadOnlyList<CellField> Fields) : CellValue;
    public sealed record Array(IReadOnlyList<CellValue> Items) : CellValue;
}

public sealed record CellField(string Name, CellValue Value);
