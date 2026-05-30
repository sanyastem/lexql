using Lexql.Core.Abstractions;
using Lexql.Providers.MySql;

namespace Lexql.Core.Tests.Providers;

public class SshTunnelSettingsTests
{
    private static ConnectionProfile Profile(params (string Key, string? Value)[] settings) =>
        new("mysql", "test", settings.ToDictionary(s => s.Key, s => s.Value));

    [Fact]
    public void FromProfile_NoSshHost_ReturnsNull()
    {
        Assert.Null(SshTunnelSettings.FromProfile(Profile(("host", "db"))));
    }

    [Fact]
    public void FromProfile_ParsesSshSettings()
    {
        var settings = SshTunnelSettings.FromProfile(Profile(
            (SshTunnelSettings.HostKey, "bastion"),
            (SshTunnelSettings.PortKey, "2222"),
            (SshTunnelSettings.UserKey, "deploy"),
            (SshTunnelSettings.PasswordKey, "pw")));

        Assert.NotNull(settings);
        Assert.Equal("bastion", settings.Host);
        Assert.Equal(2222, settings.Port);
        Assert.Equal("deploy", settings.User);
        Assert.Equal("pw", settings.Password);
    }

    [Fact]
    public void FromProfile_DefaultsPortTo22()
    {
        var settings = SshTunnelSettings.FromProfile(Profile((SshTunnelSettings.HostKey, "bastion")));

        Assert.Equal(SshTunnelSettings.DefaultPort, settings!.Port);
    }
}
