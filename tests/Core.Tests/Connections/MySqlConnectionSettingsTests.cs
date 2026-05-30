using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using Lexql.Providers.MySql;
using MySqlConnector;

namespace Lexql.Core.Tests.Connections;

public class MySqlConnectionSettingsTests
{
    private static ConnectionProfile Profile(params (string Key, string? Value)[] settings) =>
        new("mysql", "test", settings.ToDictionary(s => s.Key, s => s.Value));

    [Fact]
    public void FromProfile_MapsFieldsAndDefaults()
    {
        var profile = Profile(
            (MySqlConnectionSettings.HostKey, "db.example.com"),
            (MySqlConnectionSettings.UserKey, "app"));

        var settings = MySqlConnectionSettings.FromProfile(profile);

        Assert.Equal("db.example.com", settings.Host);
        Assert.Equal("app", settings.User);
        Assert.Equal(MySqlConnectionSettings.DefaultPort, settings.Port);
        Assert.Equal(MySqlSslMode.Preferred, settings.SslMode);
        Assert.Null(settings.Password);
        Assert.Null(settings.Database);
    }

    [Fact]
    public void FromProfile_ParsesAllProvidedFields()
    {
        var profile = Profile(
            (MySqlConnectionSettings.HostKey, "127.0.0.1"),
            (MySqlConnectionSettings.PortKey, "3307"),
            (MySqlConnectionSettings.UserKey, "root"),
            (MySqlConnectionSettings.PasswordKey, "secret"),
            (MySqlConnectionSettings.DatabaseKey, "shop"),
            (MySqlConnectionSettings.SslModeKey, "Required"));

        var settings = MySqlConnectionSettings.FromProfile(profile);

        Assert.Equal(3307, settings.Port);
        Assert.Equal("secret", settings.Password);
        Assert.Equal("shop", settings.Database);
        Assert.Equal(MySqlSslMode.Required, settings.SslMode);
    }

    [Fact]
    public void FromProfile_MissingHostAndUser_Throws()
    {
        var ex = Assert.Throws<InvalidConnectionProfileException>(
            () => MySqlConnectionSettings.FromProfile(Profile()));

        Assert.Equal("test", ex.ProfileName);
        Assert.Contains(ex.Errors, e => e.Contains("Host"));
        Assert.Contains(ex.Errors, e => e.Contains("User"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("70000")]
    [InlineData("abc")]
    public void FromProfile_InvalidPort_Throws(string port)
    {
        var profile = Profile(
            (MySqlConnectionSettings.HostKey, "h"),
            (MySqlConnectionSettings.UserKey, "u"),
            (MySqlConnectionSettings.PortKey, port));

        var ex = Assert.Throws<InvalidConnectionProfileException>(
            () => MySqlConnectionSettings.FromProfile(profile));

        Assert.Contains(ex.Errors, e => e.Contains("Port"));
    }

    [Fact]
    public void FromProfile_UnknownSslMode_Throws()
    {
        var profile = Profile(
            (MySqlConnectionSettings.HostKey, "h"),
            (MySqlConnectionSettings.UserKey, "u"),
            (MySqlConnectionSettings.SslModeKey, "Nope"));

        var ex = Assert.Throws<InvalidConnectionProfileException>(
            () => MySqlConnectionSettings.FromProfile(profile));

        Assert.Contains(ex.Errors, e => e.Contains("SSL"));
    }

    [Fact]
    public void ToConnectionString_IncludesCoreFields()
    {
        var profile = Profile(
            (MySqlConnectionSettings.HostKey, "127.0.0.1"),
            (MySqlConnectionSettings.PortKey, "3307"),
            (MySqlConnectionSettings.UserKey, "root"),
            (MySqlConnectionSettings.DatabaseKey, "shop"));

        var connectionString = MySqlConnectionSettings.FromProfile(profile).ToConnectionString();
        var parsed = new MySqlConnectionStringBuilder(connectionString);

        Assert.Equal("127.0.0.1", parsed.Server);
        Assert.Equal(3307u, parsed.Port);
        Assert.Equal("root", parsed.UserID);
        Assert.Equal("shop", parsed.Database);
    }
}
