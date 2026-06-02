using Lexql.Core.Abstractions;
using Lexql.Core.Relational;
using Lexql.Core.Relational.Tree;
using Lexql.Providers.MySql;
using Lexql.Providers.MySql.Execution;
using MySqlConnector;

namespace Lexql.Core.Tests.Providers;

public class MySqlDatabaseProviderTests
{
    private readonly MySqlDatabaseProvider _provider = new();

    [Fact]
    public void Metadata_IsMySql()
    {
        Assert.Equal("mysql", _provider.Id);
        Assert.Equal("MySQL", _provider.DisplayName);
    }

    [Theory]
    [InlineData(ProviderCapabilities.QueryText)]
    [InlineData(ProviderCapabilities.ObjectExplorer)]
    [InlineData(ProviderCapabilities.Transactions)]
    [InlineData(ProviderCapabilities.RelationalSchema)]
    public void Capabilities_DeclareSupportedFeatures(ProviderCapabilities flag)
    {
        Assert.True(_provider.Capabilities.HasFlag(flag));
    }

    [Fact]
    public void DescribeConnectionFields_IncludesRequiredHostAndUser()
    {
        var fields = _provider.DescribeConnectionFields();

        Assert.Contains(fields, f => f.Key == "host" && f.Required);
        Assert.Contains(fields, f => f.Key == "user" && f.Required);
        Assert.Contains(fields, f => f.Key == "password" && f.Kind == ConnectionFieldKind.Password);
    }

    [Fact]
    public async Task GetService_RoutesDeclaredCapabilityServices()
    {
        const string connectionString = "Server=localhost;User Id=root";
        await using var connection = new MySqlConnection(connectionString);
        await using var session = new MySqlDatabaseConnection(
            _provider, connection, connectionString, isReadOnly: false, new MySqlQueryExecutorOptions());

        Assert.NotNull(session.ObjectExplorer);
        Assert.NotNull(session.QueryExecutor);
        Assert.NotNull(session.GetService<IObjectExplorer>());
        Assert.NotNull(session.GetService<IQueryExecutor>());
        Assert.NotNull(session.GetService<IPagedQueryExecutor>());
        Assert.NotNull(session.GetService<IRelationalSchemaReader>());
        Assert.NotNull(session.GetService<IRelationalCatalog>());
    }
}
