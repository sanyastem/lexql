namespace Lexql.App.Services;

public sealed class ThemeState
{
    public string Current { get; private set; } = "dark";

    public bool IsDark => Current != "light";

    public event Action? Changed;

    public void Initialize(string theme) => Apply(theme);

    public void Set(string theme) => Apply(theme);

    public string Toggle()
    {
        Apply(IsDark ? "light" : "dark");
        return Current;
    }

    private void Apply(string theme)
    {
        var normalized = theme == "light" ? "light" : "dark";
        if (normalized == Current)
        {
            return;
        }

        Current = normalized;
        Changed?.Invoke();
    }
}
