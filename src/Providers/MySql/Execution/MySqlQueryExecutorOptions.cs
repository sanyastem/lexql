namespace Lexql.Providers.MySql.Execution;

public sealed record MySqlQueryExecutorOptions(
    int? CommandTimeoutSeconds = 30,
    int? DefaultRowLimit = null,
    bool ReadOnly = false);
