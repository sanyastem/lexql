using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Dml;
using Lexql.Providers.MySql;
using Lexql.Providers.MySql.Execution;
using Lexql.Providers.MySql.Metadata;
using MySqlConnector;

namespace Lexql.Integration.Tests;

public abstract class MySqlIntegrationTests
{
    private readonly MySqlFixture _fixture;

    protected MySqlIntegrationTests(MySqlFixture fixture) => _fixture = fixture;

    protected abstract MySqlServerFamily ExpectedFamily { get; }

    private MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(_fixture.ConnectionString);
        return connection;
    }

    [Fact]
    public async Task SchemaReader_ReadsTablesColumnsKeysAndForeignKeys()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        var schema = await new MySqlSchemaReader(connection).LoadAsync(_fixture.Database, CancellationToken.None);

        Assert.Contains(schema.Tables, t => t.Name == "users");
        var orderItems = schema.Tables.Single(t => t.Name == "order_items");
        Assert.Equal(["order_id", "item_id"], orderItems.PrimaryKey);
        Assert.Contains(orderItems.ForeignKeys, fk => fk.RefTable == "orders" && fk.Columns.Contains("order_id"));

        var users = schema.Tables.Single(t => t.Name == "users");
        Assert.Contains(users.Columns, c => c.Name == "email");
        Assert.Equal(["id"], users.PrimaryKey);
    }

    [Fact]
    public async Task QueryExecutor_SelectReturnsTypedRows()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        var executor = new MySqlQueryExecutor(connection);

        var execution = await executor.ExecuteAsync(
            new QueryRequest("SELECT id, email FROM users ORDER BY id"), CancellationToken.None);

        var resultSet = Assert.Single(execution.ResultSets);
        var rows = new List<IRecord>();
        await foreach (var record in resultSet.ReadAsync(CancellationToken.None))
        {
            rows.Add(record);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal(CellKind.Integer, resultSet.Fields[0].Kind);
        var firstEmail = Assert.IsType<CellValue.Scalar>(rows[0]["email"]);
        Assert.Equal("a@x", firstEmail.Value);
    }

    [Fact]
    public async Task DmlApplier_InsertUpdateDelete_RoundTrips()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        var executor = new MySqlQueryExecutor(connection);

        var insert = new RowChange(
            RowChangeKind.Insert, "users",
            new Dictionary<string, object?> { ["email"] = "dml@test" },
            new Dictionary<string, object?>());
        await DmlApplier.ApplyAsync(executor, DmlGenerator.Generate([insert]), CancellationToken.None);

        var inserted = await ScalarAsync(executor, "SELECT id FROM users WHERE email = 'dml@test'");
        var id = Convert.ToInt32(inserted);

        var update = new RowChange(
            RowChangeKind.Update, "users",
            new Dictionary<string, object?> { ["email"] = "dml2@test" },
            new Dictionary<string, object?> { ["id"] = id });
        await DmlApplier.ApplyAsync(executor, DmlGenerator.Generate([update]), CancellationToken.None);

        Assert.Equal("dml2@test", await ScalarAsync(executor, $"SELECT email FROM users WHERE id = {id}"));

        var delete = new RowChange(
            RowChangeKind.Delete, "users",
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["id"] = id });
        await DmlApplier.ApplyAsync(executor, DmlGenerator.Generate([delete]), CancellationToken.None);

        var remaining = await ScalarAsync(executor, $"SELECT COUNT(*) FROM users WHERE id = {id}");
        Assert.Equal(0L, Convert.ToInt64(remaining));
    }

    [Fact]
    public async Task QueryExecutor_Cancel_InterruptsLongQueryAndConnectionRecovers()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        var executor = new MySqlQueryExecutor(connection);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await executor.ExecuteAsync(new QueryRequest("SELECT SLEEP(10)"), cts.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or QueryExecutionException)
        {
        }

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8), $"query ran for {stopwatch.Elapsed}");

        var recovered = await executor.ExecuteAsync(new QueryRequest("SELECT 1"), CancellationToken.None);
        Assert.Single(recovered.ResultSets);
    }

    [Fact]
    public async Task ConnectionOpener_Probe_ReportsServerFamily()
    {
        var probe = await new MySqlConnectionOpener().TestAsync(_fixture.Profile(), CancellationToken.None);

        Assert.True(probe.Success, probe.Error);
        Assert.Equal(ExpectedFamily, MySqlServerVersion.Classify(probe.Server!.Parsed));
    }

    [Fact]
    public async Task Catalog_ListsSeededDatabase()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        var namespaces = await new MySqlCatalog(connection).ListNamespacesAsync(CancellationToken.None);

        Assert.Contains(_fixture.Database, namespaces);
    }

    [Fact]
    public async Task ServerInfo_ReportsVersionAndUser()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        var info = await new MySqlServerInfo(connection).GetServerInfoAsync(CancellationToken.None);

        Assert.Contains(info, i => i.Label == "Version" && i.Value.Length > 0);
        Assert.Contains(info, i => i.Label == "User" && i.Value.Length > 0);
    }

    private static async Task<object?> ScalarAsync(MySqlQueryExecutor executor, string sql)
    {
        var execution = await executor.ExecuteAsync(new QueryRequest(sql), CancellationToken.None);
        await foreach (var record in execution.ResultSets[0].ReadAsync(CancellationToken.None))
        {
            return (record[0] as CellValue.Scalar)?.Value;
        }

        return null;
    }
}

[CollectionDefinition("mysql57")]
public sealed class MySql57Collection : ICollectionFixture<MySql57Fixture>;

[CollectionDefinition("mysql80")]
public sealed class MySql80Collection : ICollectionFixture<MySql80Fixture>;

[Collection("mysql57")]
public sealed class MySql57IntegrationTests(MySql57Fixture fixture) : MySqlIntegrationTests(fixture)
{
    protected override MySqlServerFamily ExpectedFamily => MySqlServerFamily.MySql57;
}

[Collection("mysql80")]
public sealed class MySql80IntegrationTests(MySql80Fixture fixture) : MySqlIntegrationTests(fixture)
{
    protected override MySqlServerFamily ExpectedFamily => MySqlServerFamily.MySql80OrLater;
}
