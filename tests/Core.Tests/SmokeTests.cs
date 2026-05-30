using Lexql.Core.Abstractions;
using NSubstitute;

namespace Lexql.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void ConnectionProfile_StoresProviderAndDefaults()
    {
        var profile = new ConnectionProfile(
            ProviderId: "mysql",
            Name: "local",
            Settings: new Dictionary<string, string?> { ["host"] = "127.0.0.1" });

        Assert.Equal("mysql", profile.ProviderId);
        Assert.False(profile.ReadOnly);
        Assert.Null(profile.DefaultRowLimit);
        Assert.Equal("127.0.0.1", profile.Settings["host"]);
    }

    [Fact]
    public void ProviderCapabilities_AreComposableFlags()
    {
        var caps = ProviderCapabilities.QueryText | ProviderCapabilities.RelationalSchema;

        Assert.True(caps.HasFlag(ProviderCapabilities.QueryText));
        Assert.True(caps.HasFlag(ProviderCapabilities.RelationalSchema));
        Assert.False(caps.HasFlag(ProviderCapabilities.DocumentStore));
    }

    [Fact]
    public void DatabaseProvider_CanBeMocked()
    {
        var provider = Substitute.For<IDatabaseProvider>();
        provider.Id.Returns("mysql");
        provider.Capabilities.Returns(ProviderCapabilities.QueryText);

        Assert.Equal("mysql", provider.Id);
        Assert.True(provider.Capabilities.HasFlag(ProviderCapabilities.QueryText));
    }
}
