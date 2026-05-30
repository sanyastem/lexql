using System.Text.Json;
using Lexql.Core.Localization;
using Microsoft.Maui.Storage;

namespace Lexql.App.Services;

public sealed class LocalizationService
{
    private const string FallbackLanguage = "en";

    private Localizer? _localizer;

    public string CurrentLanguage { get; private set; } = FallbackLanguage;

    public IReadOnlyList<(string Code, string Name)> Languages { get; } =
    [
        ("en", "English"),
        ("ru", "Русский"),
        ("pl", "Polski"),
    ];

    public event Action? Changed;

    public string this[string key] => _localizer?.Get(CurrentLanguage, key) ?? key;

    public async Task InitializeAsync()
    {
        var languages = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        foreach (var (code, _) in Languages)
        {
            languages[code] = await LoadAsync(code);
        }

        _localizer = new Localizer(languages, FallbackLanguage);
        Changed?.Invoke();
    }

    public void SetLanguage(string code)
    {
        if (code == CurrentLanguage)
        {
            return;
        }

        CurrentLanguage = code;
        Changed?.Invoke();
    }

    private static async Task<IReadOnlyDictionary<string, string>> LoadAsync(string code)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync($"i18n/{code}.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }
}
