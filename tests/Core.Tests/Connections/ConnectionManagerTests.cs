using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using NSubstitute;

namespace Lexql.Core.Tests.Connections;

public class ConnectionManagerTests
{
    private static ConnectionProfile Profile(string providerId) =>
        new(providerId, "test", new Dictionary<string, string?>());

    private static IConnectionOpener Opener(string providerId)
    {
        var opener = Substitute.For<IConnectionOpener>();
        opener.ProviderId.Returns(providerId);
        return opener;
    }

    [Fact]
    public async Task OpenAsync_DelegatesToMatchingOpener()
    {
        var opener = Opener("mysql");
        var connection = Substitute.For<ILiveConnection>();
        var profile = Profile("mysql");
        var token = new CancellationToken();
        opener.OpenAsync(profile, token).Returns(connection);

        var manager = new ConnectionManager([opener]);
        var result = await manager.OpenAsync(profile, token);

        Assert.Same(connection, result);
        await opener.Received(1).OpenAsync(profile, token);
    }

    [Fact]
    public async Task TestAsync_DelegatesToMatchingOpener()
    {
        var opener = Opener("mysql");
        var probe = ConnectionProbe.Ok(new ServerVersionInfo("8.0.36", new Version(8, 0, 36)), TimeSpan.Zero);
        var profile = Profile("mysql");
        opener.TestAsync(profile, Arg.Any<CancellationToken>()).Returns(probe);

        var manager = new ConnectionManager([opener]);
        var result = await manager.TestAsync(profile, CancellationToken.None);

        Assert.Same(probe, result);
    }

    [Fact]
    public async Task ResolveByProviderId_IsCaseInsensitive()
    {
        var opener = Opener("mysql");
        var connection = Substitute.For<ILiveConnection>();
        opener.OpenAsync(Arg.Any<ConnectionProfile>(), Arg.Any<CancellationToken>()).Returns(connection);
        var manager = new ConnectionManager([opener]);

        var result = await manager.OpenAsync(Profile("MySQL"), CancellationToken.None);

        Assert.Same(connection, result);
    }

    [Fact]
    public async Task UnknownProvider_Throws()
    {
        var manager = new ConnectionManager([Opener("mysql")]);

        var ex = await Assert.ThrowsAsync<UnknownProviderException>(
            () => manager.OpenAsync(Profile("postgres"), CancellationToken.None));

        Assert.Equal("postgres", ex.ProviderId);
    }

    [Fact]
    public void DuplicateOpeners_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new ConnectionManager([Opener("mysql"), Opener("mysql")]));
    }
}
