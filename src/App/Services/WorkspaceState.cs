using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using ConnectionProfile = Lexql.Core.Abstractions.ConnectionProfile;

namespace Lexql.App.Services;

public sealed class WorkspaceState
{
    private readonly IDatabaseProvider _provider;
    private readonly IConnectionProfileStore _store;

    public WorkspaceState(IDatabaseProvider provider, IConnectionProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(store);
        _provider = provider;
        _store = store;
    }

    public List<ServerConnection> Connections { get; } = [];

    public List<QueryTab> Tabs { get; } = [];

    public List<ConnectionProfile> SavedProfiles { get; private set; } = [];

    public string? ActiveConnectionId { get; private set; }

    public string? ActiveTabId { get; private set; }

    public string? LastError { get; private set; }

    public string TreeFilter { get; private set; } = string.Empty;

    public void SetTreeFilter(string? value)
    {
        TreeFilter = value ?? string.Empty;
        Notify();
    }

    public string ProviderId => _provider.Id;

    public IReadOnlyList<ConnectionField> Fields => _provider.DescribeConnectionFields();

    public ServerConnection? Active => Connections.FirstOrDefault(c => c.Id == ActiveConnectionId);

    public QueryTab? ActiveTab => Tabs.FirstOrDefault(t => t.Id == ActiveTabId);

    public event Action? Changed;

    public Func<Task>? FlushActiveTab { get; set; }

    public async Task SwitchTabAsync(string id)
    {
        if (id == ActiveTabId)
        {
            return;
        }

        if (FlushActiveTab is not null)
        {
            await FlushActiveTab();
        }

        ActiveTabId = id;
        Notify();
    }

    public async Task InitializeAsync()
    {
        SavedProfiles = (await _store.LoadAllAsync(CancellationToken.None)).ToList();
        Notify();
    }

    public void SetActiveConnection(string id)
    {
        ActiveConnectionId = id;
        Notify();
    }

    public void SetActiveTab(string id)
    {
        ActiveTabId = id;
        Notify();
    }

    public ServerConnection? ConnectionFor(QueryTab tab) =>
        tab.ConnectionId is null ? null : Connections.FirstOrDefault(c => c.Id == tab.ConnectionId);

    public async Task<bool> ConnectAsync(ConnectionProfile profile)
    {
        LastError = null;
        try
        {
            var connection = await _provider.ConnectAsync(profile, CancellationToken.None);
            profile.Settings.TryGetValue("database", out var database);
            var server = new ServerConnection
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = profile.Name,
                Connection = connection,
                DefaultNamespace = string.IsNullOrWhiteSpace(database) ? null : database,
                Roots = await connection.ObjectExplorer.GetRootsAsync(CancellationToken.None),
            };

            Connections.Add(server);
            ActiveConnectionId = server.Id;

            if (Tabs.Count == 0)
            {
                AddTab(server.Id);
            }
            else if (ActiveTab is { ConnectionId: null } tab)
            {
                tab.ConnectionId = server.Id;
                tab.Database = server.DefaultNamespace;
            }

            Notify();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Notify();
            return false;
        }
    }

    public async Task SaveProfileAsync(ConnectionProfile profile)
    {
        SavedProfiles.RemoveAll(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
        SavedProfiles.Add(profile);
        await _store.SaveAllAsync(SavedProfiles, CancellationToken.None);
        Notify();
    }

    public async Task<IReadOnlyList<ServerInfoItem>> GetServerInfoAsync(string connectionId, CancellationToken ct)
    {
        var server = Connections.FirstOrDefault(c => c.Id == connectionId);
        var info = server?.Connection.GetService<IServerInfoProvider>();
        return info is null ? [] : await info.GetServerInfoAsync(ct);
    }

    public async Task RefreshConnectionAsync(string connectionId)
    {
        var server = Connections.FirstOrDefault(c => c.Id == connectionId);
        if (server is null)
        {
            return;
        }

        server.Roots = await server.Connection.ObjectExplorer.GetRootsAsync(CancellationToken.None);
        server.RefreshToken++;
        Notify();
    }

    public async Task RefreshAllAsync()
    {
        SavedProfiles = (await _store.LoadAllAsync(CancellationToken.None)).ToList();

        foreach (var server in Connections)
        {
            server.Roots = await server.Connection.ObjectExplorer.GetRootsAsync(CancellationToken.None);
            server.RefreshToken++;
        }

        Notify();
    }

    public async Task DisconnectAsync(string connectionId)
    {
        var server = Connections.FirstOrDefault(c => c.Id == connectionId);
        if (server is null)
        {
            return;
        }

        Connections.Remove(server);
        await server.Connection.DisposeAsync();

        foreach (var tab in Tabs.Where(t => t.ConnectionId == connectionId))
        {
            tab.ConnectionId = null;
        }

        if (ActiveConnectionId == connectionId)
        {
            ActiveConnectionId = Connections.FirstOrDefault()?.Id;
        }

        Notify();
    }

    public QueryTab AddTab(string? connectionId = null)
    {
        var id = connectionId ?? ActiveConnectionId;
        var tab = new QueryTab
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnectionId = id,
            Database = Connections.FirstOrDefault(c => c.Id == id)?.DefaultNamespace,
            Title = $"Query {Tabs.Count + 1}",
        };

        Tabs.Add(tab);
        ActiveTabId = tab.Id;
        Notify();
        return tab;
    }

    public QueryTab OpenFileTab(string path, string content)
    {
        var id = ActiveConnectionId;
        var tab = new QueryTab
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnectionId = id,
            Database = Connections.FirstOrDefault(c => c.Id == id)?.DefaultNamespace,
            Title = Path.GetFileName(path),
            FilePath = path,
            Sql = content,
            Dirty = false,
        };

        Tabs.Add(tab);
        ActiveTabId = tab.Id;
        Notify();
        return tab;
    }

    public QueryTab OpenTableData(string connectionId, string schema, string table)
    {
        var sql = $"SELECT * FROM `{schema.Replace("`", "``")}`.`{table.Replace("`", "``")}`";
        var tab = new QueryTab
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnectionId = connectionId,
            Database = schema,
            Title = table,
            Sql = sql,
            PendingRun = true,
        };

        Tabs.Add(tab);
        ActiveTabId = tab.Id;
        Notify();
        return tab;
    }

    public void CloseTab(string tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null)
        {
            return;
        }

        Tabs.Remove(tab);
        if (ActiveTabId == tabId)
        {
            ActiveTabId = Tabs.LastOrDefault()?.Id;
        }

        Notify();
    }

    public IReadOnlyList<QueryTab> ClosableTabs(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        return Tabs.Where(t => wanted.Contains(t.Id) && !HasUnsavedWork(t)).ToList();
    }

    public IReadOnlyList<string> CloseTabs(IEnumerable<string> ids)
    {
        var closable = ClosableTabs(ids);
        foreach (var tab in closable)
        {
            Tabs.Remove(tab);
        }

        if (ActiveTabId is { } active && Tabs.All(t => t.Id != active))
        {
            ActiveTabId = Tabs.LastOrDefault()?.Id;
        }

        Notify();
        return Tabs.Where(t => ids.Contains(t.Id)).Select(t => t.Id).ToList();
    }

    public IEnumerable<string> OtherTabIds(string keepId) =>
        Tabs.Where(t => t.Id != keepId).Select(t => t.Id);

    public IEnumerable<string> TabIdsRightOf(string tabId)
    {
        var index = Tabs.FindIndex(t => t.Id == tabId);
        return index < 0 ? [] : Tabs.Skip(index + 1).Select(t => t.Id);
    }

    private static bool HasUnsavedWork(QueryTab tab) => tab.Dirty && !string.IsNullOrWhiteSpace(tab.Sql);

    private void Notify() => Changed?.Invoke();
}

public readonly record struct ObjectRef(string Namespace, string Name);
