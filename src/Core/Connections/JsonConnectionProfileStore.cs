using System.Text.Json;
using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public sealed class JsonConnectionProfileStore : IConnectionProfileStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;
    private readonly IReadOnlySet<string> _secretKeys;

    public JsonConnectionProfileStore(string path, IReadOnlySet<string> secretKeys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(secretKeys);
        _path = path;
        _secretKeys = secretKeys;
    }

    public async Task<IReadOnlyList<ConnectionProfile>> LoadAllAsync(CancellationToken ct)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        await using var stream = File.OpenRead(_path);
        var dtos = await JsonSerializer.DeserializeAsync<List<ProfileDto>>(stream, Options, ct) ?? [];
        return dtos.Select(ToProfile).ToList();
    }

    public async Task SaveAllAsync(IReadOnlyList<ConnectionProfile> profiles, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dtos = profiles.Select(ToDto).ToList();
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, dtos, Options, ct);
    }

    private ProfileDto ToDto(ConnectionProfile profile) => new(
        profile.ProviderId,
        profile.Name,
        profile.Settings
            .Where(setting => !_secretKeys.Contains(setting.Key))
            .ToDictionary(setting => setting.Key, setting => setting.Value),
        profile.ReadOnly,
        profile.DefaultRowLimit);

    private static ConnectionProfile ToProfile(ProfileDto dto) => new(
        dto.ProviderId,
        dto.Name,
        dto.Settings ?? new Dictionary<string, string?>(),
        dto.ReadOnly,
        dto.DefaultRowLimit);

    private sealed record ProfileDto(
        string ProviderId,
        string Name,
        Dictionary<string, string?>? Settings,
        bool ReadOnly,
        int? DefaultRowLimit);
}
