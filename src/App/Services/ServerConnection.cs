using Lexql.Core.Abstractions;

namespace Lexql.App.Services;

public sealed class ServerConnection
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IDatabaseConnection Connection { get; init; }

    public string? DefaultNamespace { get; init; }

    public IReadOnlyList<DatabaseObjectNode> Roots { get; set; } = [];
}
