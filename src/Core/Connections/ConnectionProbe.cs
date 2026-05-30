namespace Lexql.Core.Connections;

public sealed record ConnectionProbe(bool Success, ServerVersionInfo? Server, TimeSpan Elapsed, string? Error)
{
    public static ConnectionProbe Ok(ServerVersionInfo server, TimeSpan elapsed) =>
        new(true, server, elapsed, null);

    public static ConnectionProbe Failed(string error, TimeSpan elapsed) =>
        new(false, null, elapsed, error);
}
