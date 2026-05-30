using Lexql.Core.History;
using Microsoft.Data.Sqlite;

namespace Lexql.Core.Tests.History;

public class SqliteQueryHistoryStoreTests : IAsyncLifetime
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "lexql-hist-" + Guid.NewGuid().ToString("N"));
    private SqliteQueryHistoryStore _store = default!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dir);
        _store = new SqliteQueryHistoryStore(Path.Combine(_dir, "history.db"));
        await _store.InitializeAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task History_AddedAndSearchable()
    {
        await _store.AddHistoryAsync("SELECT * FROM users", "local", true, CancellationToken.None);
        await _store.AddHistoryAsync("DELETE FROM orders", "local", false, CancellationToken.None);

        var all = await _store.SearchHistoryAsync(null, 10, CancellationToken.None);
        Assert.Equal(2, all.Count);
        Assert.Equal("DELETE FROM orders", all[0].Sql);

        var filtered = await _store.SearchHistoryAsync("users", 10, CancellationToken.None);
        Assert.Equal("SELECT * FROM users", Assert.Single(filtered).Sql);
    }

    [Fact]
    public async Task History_LimitApplies()
    {
        for (var i = 0; i < 5; i++)
        {
            await _store.AddHistoryAsync($"SELECT {i}", null, true, CancellationToken.None);
        }

        Assert.Equal(2, (await _store.SearchHistoryAsync(null, 2, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task SavedQuery_UpsertsByName()
    {
        await _store.SaveQueryAsync("daily", "SELECT 1", CancellationToken.None);
        await _store.SaveQueryAsync("daily", "SELECT 2", CancellationToken.None);

        var saved = Assert.Single(await _store.ListSavedAsync(CancellationToken.None));
        Assert.Equal("daily", saved.Name);
        Assert.Equal("SELECT 2", saved.Sql);
    }

    [Fact]
    public async Task SavedQuery_Delete()
    {
        await _store.SaveQueryAsync("q", "SELECT 1", CancellationToken.None);
        var saved = Assert.Single(await _store.ListSavedAsync(CancellationToken.None));

        await _store.DeleteSavedAsync(saved.Id, CancellationToken.None);

        Assert.Empty(await _store.ListSavedAsync(CancellationToken.None));
    }
}
