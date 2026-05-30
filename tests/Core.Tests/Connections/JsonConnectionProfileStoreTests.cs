using Lexql.Core.Abstractions;
using Lexql.Core.Connections;

namespace Lexql.Core.Tests.Connections;

public class JsonConnectionProfileStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "lexql-test-" + Guid.NewGuid().ToString("N"));

    private JsonConnectionProfileStore Store() =>
        new(Path.Combine(_dir, "connections.json"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "password" },
            new AesSecretProtector(AesSecretProtector.CreateKey()));

    [Fact]
    public async Task LoadAll_MissingFile_ReturnsEmpty()
    {
        Assert.Empty(await Store().LoadAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsMetadataAndPassword()
    {
        var profile = new ConnectionProfile(
            "mysql",
            "local",
            new Dictionary<string, string?> { ["host"] = "127.0.0.1", ["user"] = "root", ["password"] = "secret" },
            ReadOnly: true,
            DefaultRowLimit: 500);

        var store = Store();
        await store.SaveAllAsync([profile], CancellationToken.None);
        var loaded = Assert.Single(await store.LoadAllAsync(CancellationToken.None));

        Assert.Equal("local", loaded.Name);
        Assert.Equal("127.0.0.1", loaded.Settings["host"]);
        Assert.True(loaded.ReadOnly);
        Assert.Equal(500, loaded.DefaultRowLimit);
        Assert.Equal("secret", loaded.Settings["password"]);
    }

    [Fact]
    public async Task SavedFile_DoesNotContainPlaintextPassword()
    {
        var profile = new ConnectionProfile(
            "mysql", "local",
            new Dictionary<string, string?> { ["password"] = "topsecret" });

        var path = Path.Combine(_dir, "connections.json");
        await Store().SaveAllAsync([profile], CancellationToken.None);

        Assert.DoesNotContain("topsecret", await File.ReadAllTextAsync(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
