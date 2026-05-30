using Lexql.Providers.MySql;

namespace Lexql.Core.Tests.Connections;

public class MySqlServerVersionTests
{
    [Theory]
    [InlineData("8.0.36", 8, 0, 36)]
    [InlineData("5.7.44-log", 5, 7, 44)]
    [InlineData("8.4.0-mysql", 8, 4, 0)]
    public void Parse_ExtractsNumericVersion(string raw, int major, int minor, int build)
    {
        var version = MySqlServerVersion.Parse(raw);

        Assert.NotNull(version);
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void Parse_ReturnsNullForGarbage(string? raw)
    {
        Assert.Null(MySqlServerVersion.Parse(raw));
    }

    [Theory]
    [InlineData("5.7.44", MySqlServerFamily.MySql57)]
    [InlineData("8.0.36", MySqlServerFamily.MySql80OrLater)]
    [InlineData("8.4.0", MySqlServerFamily.MySql80OrLater)]
    [InlineData("5.6.51", MySqlServerFamily.Unknown)]
    public void Classify_MapsSupportedFamilies(string raw, MySqlServerFamily expected)
    {
        Assert.Equal(expected, MySqlServerVersion.Classify(MySqlServerVersion.Parse(raw)));
    }

    [Fact]
    public void Classify_NullIsUnknown()
    {
        Assert.Equal(MySqlServerFamily.Unknown, MySqlServerVersion.Classify(null));
    }
}
