using Lexql.Core.Abstractions;
using MySqlConnector;
using Testcontainers.MySql;

namespace Lexql.Integration.Tests;

public abstract class MySqlFixture : IAsyncLifetime
{
    private static readonly string[] SeedStatements =
    [
        "CREATE TABLE orders (id INT PRIMARY KEY AUTO_INCREMENT, total DECIMAL(10,2) NOT NULL)",
        "CREATE TABLE users (id INT PRIMARY KEY AUTO_INCREMENT, email VARCHAR(255) NULL, INDEX ix_email (email))",
        "CREATE TABLE order_items (order_id INT, item_id INT, qty INT NOT NULL, " +
            "PRIMARY KEY (order_id, item_id), " +
            "CONSTRAINT fk_oi_order FOREIGN KEY (order_id) REFERENCES orders(id))",
        "INSERT INTO users (email) VALUES ('a@x'), ('b@y')",
        "INSERT INTO orders (total) VALUES (10.50)",
    ];

    private readonly MySqlContainer _container;

    protected MySqlFixture(string image)
    {
        _container = new MySqlBuilder(image)
            .WithDatabase(Database)
            .WithUsername(User)
            .WithPassword(Password)
            .Build();
    }

    public string Database => "lexql";

    public string User => "app";

    public string Password => "app";

    public string ConnectionString =>
        new MySqlConnectionStringBuilder(_container.GetConnectionString()) { SslMode = MySqlSslMode.None }.ConnectionString;

    public string Host => _container.Hostname;

    public int Port => _container.GetMappedPublicPort(MySqlBuilder.MySqlPort);

    public ConnectionProfile Profile() => new(
        "mysql",
        "integration",
        new Dictionary<string, string?>
        {
            ["host"] = Host,
            ["port"] = Port.ToString(),
            ["user"] = User,
            ["password"] = Password,
            ["database"] = Database,
            ["sslMode"] = "None",
        });

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        foreach (var statement in SeedStatements)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public sealed class MySql57Fixture() : MySqlFixture("mysql:5.7");

public sealed class MySql80Fixture() : MySqlFixture("mysql:8.0");
