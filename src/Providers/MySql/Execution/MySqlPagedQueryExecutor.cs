using System.Data;
using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Execution;
using Lexql.Core.Relational.Sql;
using MySqlConnector;

namespace Lexql.Providers.MySql.Execution;

public sealed class MySqlPagedQueryExecutor : IPagedQueryExecutor
{
    private readonly string _connectionString;
    private readonly MySqlQueryExecutorOptions _options;

    public MySqlPagedQueryExecutor(string connectionString, MySqlQueryExecutorOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = connectionString;
        _options = options;
    }

    public async Task<IQueryCursor> OpenAsync(QueryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_options.ReadOnly && !SqlStatementClassifier.IsAllowedInReadOnly(request.Text))
        {
            throw new ReadOnlyViolationException();
        }

        var connection = new MySqlConnection(_connectionString);
        MySqlCommand? command = null;
        MySqlDataReader? reader = null;

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

        try
        {
            await connection.OpenAsync(ct);
            connection.InfoMessage += OnInfoMessage;

            command = connection.CreateCommand();
            command.CommandText = MySqlQueryExecutor.Tag(request.Text);
            if (_options.CommandTimeoutSeconds is { } timeout)
            {
                command.CommandTimeout = timeout;
            }

            AddParameters(command, request.Parameters);

            reader = await command.ExecuteReaderAsync(ct);
            var affected = reader.RecordsAffected >= 0 ? reader.RecordsAffected : (long?)null;
            var owner = new CursorOwner(connection, command, () => connection.InfoMessage -= OnInfoMessage);
            return new AdoQueryCursor(reader, owner, affected, messages);
        }
        catch (MySqlException ex)
        {
            connection.InfoMessage -= OnInfoMessage;
            await CleanupAsync(reader, command, connection);
            throw new QueryExecutionException(ex.Message, (int)ex.ErrorCode, ex.SqlState, ex);
        }
        catch
        {
            connection.InfoMessage -= OnInfoMessage;
            await CleanupAsync(reader, command, connection);
            throw;
        }
    }

    private static async Task CleanupAsync(MySqlDataReader? reader, MySqlCommand? command, MySqlConnection connection)
    {
        if (reader is not null)
        {
            await reader.DisposeAsync();
        }

        if (command is not null)
        {
            await command.DisposeAsync();
        }

        await connection.DisposeAsync();
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

    private sealed class CursorOwner : IAsyncDisposable
    {
        private readonly MySqlConnection _connection;
        private readonly MySqlCommand _command;
        private readonly Action _onDispose;

        public CursorOwner(MySqlConnection connection, MySqlCommand command, Action onDispose)
        {
            _connection = connection;
            _command = command;
            _onDispose = onDispose;
        }

        public async ValueTask DisposeAsync()
        {
            _onDispose();
            await _command.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
