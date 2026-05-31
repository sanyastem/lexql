using System.Data;
using System.Diagnostics;
using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Execution;
using Lexql.Core.Relational.Sql;
using MySqlConnector;

namespace Lexql.Providers.MySql.Execution;

public sealed class MySqlQueryExecutor : IQueryExecutor
{
    public const string TraceMarker = "/* Lexql */";

    private readonly MySqlConnection _connection;
    private readonly MySqlQueryExecutorOptions _options;

    public static string Tag(string text) => $"{TraceMarker} {text}";

    public MySqlQueryExecutor(MySqlConnection connection, MySqlQueryExecutorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _options = options ?? new MySqlQueryExecutorOptions();
    }

    public async Task<QueryExecution> ExecuteAsync(QueryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_options.ReadOnly && !SqlStatementClassifier.IsAllowedInReadOnly(request.Text))
        {
            throw new ReadOnlyViolationException();
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        using var command = _connection.CreateCommand();
        command.CommandText = Tag(request.Text);
        if (_options.CommandTimeoutSeconds is { } timeout)
        {
            command.CommandTimeout = timeout;
        }

        AddParameters(command, request.Parameters);

        var rowLimit = request.RowLimit ?? _options.DefaultRowLimit;
        var messages = new List<DiagnosticMessage>();
        void OnInfoMessage(object sender, MySqlInfoMessageEventArgs args)
        {
            foreach (var error in args.Errors)
            {
                var severity = Convert.ToString(error.Level)?.ToUpperInvariant() switch
                {
                    "ERROR" => DiagnosticSeverity.Error,
                    "WARNING" => DiagnosticSeverity.Warning,
                    _ => DiagnosticSeverity.Info,
                };
                messages.Add(new DiagnosticMessage(severity, $"[{error.ErrorCode}] {error.Message}"));
            }
        }

        var stopwatch = Stopwatch.StartNew();
        _connection.InfoMessage += OnInfoMessage;
        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            var result = await AdoResultReader.ReadAllAsync(reader, rowLimit, ct);
            stopwatch.Stop();

            return new QueryExecution
            {
                ResultSets = result.ResultSets,
                AffectedCount = result.RecordsAffected >= 0 ? result.RecordsAffected : null,
                Elapsed = stopwatch.Elapsed,
                Messages = messages,
            };
        }
        catch (MySqlException ex)
        {
            throw new QueryExecutionException(ex.Message, (int)ex.ErrorCode, ex.SqlState, ex);
        }
        finally
        {
            _connection.InfoMessage -= OnInfoMessage;
        }
    }

    private static void AddParameters(MySqlCommand command, IReadOnlyList<QueryParameter>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new MySqlParameter(parameter.Name, parameter.Value ?? DBNull.Value));
        }
    }
}
