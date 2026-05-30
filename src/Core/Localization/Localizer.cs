namespace Lexql.Core.Localization;

public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _languages;
    private readonly string _fallbackLanguage;

    public Localizer(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> languages,
        string fallbackLanguage)
    {
        ArgumentNullException.ThrowIfNull(languages);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackLanguage);
        _languages = languages;
        _fallbackLanguage = fallbackLanguage;
    }

    public IReadOnlyCollection<string> Languages => _languages.Keys.ToList();

    public string Get(string language, string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_languages.TryGetValue(language, out var map) && map.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_languages.TryGetValue(_fallbackLanguage, out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }
}
