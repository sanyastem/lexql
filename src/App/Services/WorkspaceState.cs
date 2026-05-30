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

    public string ProviderId => _provider.Id;

    public IReadOnlyList<ConnectionField> Fields => _provider.DescribeConnectionFields();

    public ServerConnection? Active => Connections.FirstOrDefault(c => c.Id == ActiveConnectionId);

    public QueryTab? ActiveTab => Tabs.FirstOrDefault(t => t.Id == ActiveTabId);

    public event Action? Changed;

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
            var server = new ServerConnection
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = profile.Name,
                Connection = connection,
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
        var tab = new QueryTab
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnectionId = connectionId ?? ActiveConnectionId,
            Title = $"Query {Tabs.Count + 1}",
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

    private void Notify() => Changed?.Invoke();
}
