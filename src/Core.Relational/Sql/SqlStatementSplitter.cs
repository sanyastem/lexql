namespace Lexql.Core.Relational.Sql;

public sealed record SqlStatement(string Text, int Start, int End);

public static class SqlStatementSplitter
{
    public static IReadOnlyList<SqlStatement> Split(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var statements = new List<SqlStatement>();
        foreach (var (start, end) in Segments(text))
        {
            var trimmed = Normalize(text, start, end);
            if (trimmed.Length > 0)
            {
                statements.Add(new SqlStatement(trimmed, start, end));
            }
        }

        return statements;
    }

    public static string? StatementAt(string text, int offset)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return null;
        }

        offset = Math.Clamp(offset, 0, text.Length);
        foreach (var (start, end) in Segments(text))
        {
            if (offset >= start && offset <= end)
            {
                var trimmed = Normalize(text, start, end);
                if (trimmed.Length > 0)
                {
                    return trimmed;
                }
            }
        }

        return null;
    }

    private static string Normalize(string text, int start, int end) =>
        text[start..end].Trim().TrimEnd(';').Trim();

    private static IEnumerable<(int Start, int End)> Segments(string text)
    {
        var start = 0;
        var quote = '\0';

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (c is '\'' or '"' or '`')
            {
                quote = c;
                continue;
            }

            if (c == ';')
            {
                yield return (start, i + 1);
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            yield return (start, text.Length);
        }
    }
}
