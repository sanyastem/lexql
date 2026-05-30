using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Sql;
using Lexql.Core.Results;

namespace Lexql.App.Services;

public sealed class QueryTab
{
    public required string Id { get; init; }

    public string Title { get; set; } = "Query";

    public string? ConnectionId { get; set; }

    public string Sql { get; set; } = "SELECT 1;";

    public int RowLimit { get; set; } = 1000;

    public ResultGridModel? Grid { get; set; }

    public EditabilityInfo? Editability { get; set; }

    public string? Status { get; set; }

    public string? Error { get; set; }

    public IReadOnlyList<DiagnosticMessage> Messages { get; set; } = [];
}
