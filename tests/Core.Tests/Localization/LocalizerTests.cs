using Lexql.Core.Localization;

namespace Lexql.Core.Tests.Localization;

public class LocalizerTests
{
    private static Localizer Build()
    {
        var languages = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string> { ["run"] = "Run", ["connect"] = "Connect" },
            ["ru"] = new Dictionary<string, string> { ["run"] = "Выполнить" },
        };
        return new Localizer(languages, "en");
    }

    [Fact]
    public void Get_ReturnsTranslationForLanguage()
    {
        Assert.Equal("Выполнить", Build().Get("ru", "run"));
    }

    [Fact]
    public void Get_FallsBackToFallbackLanguage()
    {
        Assert.Equal("Connect", Build().Get("ru", "connect"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKey()
    {
        Assert.Equal("missing", Build().Get("ru", "missing"));
    }

    [Fact]
    public void Get_UnknownLanguage_FallsBack()
    {
        Assert.Equal("Run", Build().Get("pl", "run"));
    }
}
