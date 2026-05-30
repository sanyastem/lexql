namespace Lexql.App.Services;

public sealed record GridChangeSet(
    List<GridUpdate> Updates,
    List<GridInsert> Inserts,
    List<GridDelete> Deletes);

public sealed record GridUpdate(Dictionary<string, string?> Key, Dictionary<string, string?> Values);

public sealed record GridInsert(Dictionary<string, string?> Values);

public sealed record GridDelete(Dictionary<string, string?> Key);
