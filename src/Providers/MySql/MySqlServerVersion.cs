namespace Lexql.Providers.MySql;

public enum MySqlServerFamily
{
    Unknown,
    MySql57,
    MySql80OrLater,
}

public static class MySqlServerVersion
{
    public static Version? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var length = 0;
        var dots = 0;
        foreach (var c in raw)
        {
            if (char.IsDigit(c))
            {
                length++;
            }
            else if (c == '.' && dots < 2)
            {
                dots++;
                length++;
            }
            else
            {
                break;
            }
        }

        var numeric = raw[..length];
        return Version.TryParse(numeric, out var version) ? version : null;
    }

    public static MySqlServerFamily Classify(Version? version)
    {
        if (version is null)
        {
            return MySqlServerFamily.Unknown;
        }

        if (version.Major >= 8)
        {
            return MySqlServerFamily.MySql80OrLater;
        }

        if (version is { Major: 5, Minor: 7 })
        {
            return MySqlServerFamily.MySql57;
        }

        return MySqlServerFamily.Unknown;
    }
}
