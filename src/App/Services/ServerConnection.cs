using Lexql.Core.Abstractions;

namespace Lexql.App.Services;

public sealed class ServerConnection
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IDatabaseConnection Connection { get; init; }

    public string? DefaultNamespace { get; init; }

    public IReadOnlyList<DatabaseObjectNode> Roots { get; set; } = [];

    public int RefreshToken { get; set; }

    public IReadOnlyList<ObjectIndexItem>? ObjectIndex { get; set; }

    public int ObjectIndexToken { get; set; } = -1;
}

public sealed record ObjectIndexItem(string Schema, string Name, bool IsView);
