using Lexql.Providers.MySql.Metadata;

namespace Lexql.Core.Tests.Metadata;

public class MySqlMetadataQueriesTests
{
    [Theory]
    [InlineData("8.0.16", true)]
    [InlineData("8.0.36", true)]
    [InlineData("8.4.0", true)]
    [InlineData("8.0.15", false)]
    [InlineData("5.7.44", false)]
    public void SupportsCheckConstraints_GatedAt8_0_16(string version, bool expected)
    {
        Assert.Equal(expected, MySqlMetadataQueries.SupportsCheckConstraints(Version.Parse(version)));
    }

    [Fact]
    public void SupportsCheckConstraints_NullIsFalse()
    {
        Assert.False(MySqlMetadataQueries.SupportsCheckConstraints(null));
    }
}
