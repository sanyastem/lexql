using System.Text.Json;
using Lexql.Core.Abstractions;

namespace Lexql.Core.Connections;

public sealed class JsonConnectionProfileStore : IConnectionProfileStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private const string EncryptedPrefix = "enc:";

    private readonly string _path;
    private readonly IReadOnlySet<string> _secretKeys;
    private readonly ISecretProtector _protector;

    public JsonConnectionProfileStore(string path, IReadOnlySet<string> secretKeys, ISecretProtector protector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(secretKeys);
        ArgumentNullException.ThrowIfNull(protector);
        _path = path;
        _secretKeys = secretKeys;
        _protector = protector;
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
        profile.Settings.ToDictionary(setting => setting.Key, setting => Encode(setting.Key, setting.Value)),
        profile.ReadOnly,
        profile.DefaultRowLimit);

    private ConnectionProfile ToProfile(ProfileDto dto) => new(
        dto.ProviderId,
        dto.Name,
        (dto.Settings ?? new Dictionary<string, string?>())
            .ToDictionary(setting => setting.Key, setting => Decode(setting.Key, setting.Value)),
        dto.ReadOnly,
        dto.DefaultRowLimit);

    private string? Encode(string key, string? value)
    {
        if (value is null || !_secretKeys.Contains(key))
        {
            return value;
        }

        return EncryptedPrefix + _protector.Protect(value);
    }

    private string? Decode(string key, string? value)
    {
        if (value is null || !_secretKeys.Contains(key) || !value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            return _protector.Unprotect(value[EncryptedPrefix.Length..]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed record ProfileDto(
        string ProviderId,
        string Name,
        Dictionary<string, string?>? Settings,
        bool ReadOnly,
        int? DefaultRowLimit);
}
