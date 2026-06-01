using Lexql.Core.Relational.Sql;
using Lexql.Core.Results;

namespace Lexql.App.Services;

public sealed class QueryTab
{
    public required string Id { get; init; }

    public string Title { get; set; } = "Query";

    public string? ConnectionId { get; set; }

    public string? Database { get; set; }

    public string? FilePath { get; set; }

    public bool Dirty { get; set; }

    public bool PendingRun { get; set; }

    public string Sql { get; set; } = string.Empty;

    public int RowLimit { get; set; } = 1000;

    public ResultGridModel? Grid { get; set; }

    public EditabilityInfo? Editability { get; set; }

    public List<OutputEntry> Output { get; } = [];

    public OutputEntry? LastOutput => Output.Count > 0 ? Output[^1] : null;

    public bool HasErrors => Output.Any(e => e.Kind == OutputKind.Error);
}

public enum OutputKind
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed record OutputEntry(DateTime Time, OutputKind Kind, string Text, double? ElapsedMs = null, string? Detail = null);
