using System.Text.RegularExpressions;
using Lexql.Core.Relational.Sql.Antlr;

namespace Lexql.Core.Relational.Sql;

public sealed record SqlStatement(string Text, int Start, int End);

public static partial class MySqlStatementSplitter
{
    private static readonly int[] ProtectedTypes =
    [
        MySqlLexer.STRING_LITERAL,
        MySqlLexer.START_NATIONAL_STRING_LITERAL,
        MySqlLexer.COMMENT_INPUT,
        MySqlLexer.LINE_COMMENT,
        MySqlLexer.SPEC_MYSQL_COMMENT,
    ];

    [GeneratedRegex(@"^\s*DELIMITER\s+(\S+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DelimiterDirective();

    public static IReadOnlyList<SqlStatement> SplitStatements(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var ranges = BuildProtectedRanges(sql);
        var statements = new List<SqlStatement>();
        var delimiter = ";";
        var segmentStart = 0;
        var i = 0;

        while (i < sql.Length)
        {
            if (IsProtected(ranges, i))
            {
                i++;
                continue;
            }

            if (AtLineStart(sql, i) && TryReadDelimiterDirective(sql, i, out var newDelimiter, out var lineEnd))
            {
                Add(statements, sql, segmentStart, i);
                delimiter = newDelimiter;
                i = lineEnd;
                segmentStart = lineEnd;
                continue;
            }

            if (MatchesAt(sql, i, delimiter))
            {
                Add(statements, sql, segmentStart, i);
                i += delimiter.Length;
                segmentStart = i;
                continue;
            }

            i++;
        }

        Add(statements, sql, segmentStart, sql.Length);
        return statements;
    }

    public static string? StatementAt(string sql, int caret)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var statements = SplitStatements(sql);
        if (statements.Count == 0)
        {
            return null;
        }

        caret = Math.Clamp(caret, 0, sql.Length);
        foreach (var statement in statements)
        {
            if (caret >= statement.Start && caret <= statement.End)
            {
                return statement.Text;
            }
        }

        return statements[^1].Text;
    }

    private static List<(int Start, int Stop)> BuildProtectedRanges(string sql) =>
        MySqlTokenizer.Tokenize(sql)
            .Where(t => ProtectedTypes.Contains(t.Type) && t.Stop >= t.Start)
            .Select(t => (t.Start, t.Stop))
            .OrderBy(r => r.Start)
            .ToList();

    private static bool IsProtected(List<(int Start, int Stop)> ranges, int position)
    {
        var lo = 0;
        var hi = ranges.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            var range = ranges[mid];
            if (position < range.Start)
            {
                hi = mid - 1;
            }
            else if (position > range.Stop)
            {
                lo = mid + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static bool AtLineStart(string sql, int i) => i == 0 || sql[i - 1] == '\n';

    private static bool TryReadDelimiterDirective(string sql, int lineStart, out string delimiter, out int lineEnd)
    {
        var newline = sql.IndexOf('\n', lineStart);
        var contentEnd = newline < 0 ? sql.Length : newline;
        lineEnd = newline < 0 ? sql.Length : newline + 1;

        var match = DelimiterDirective().Match(sql[lineStart..contentEnd]);
        if (match.Success)
        {
            delimiter = match.Groups[1].Value;
            return true;
        }

        delimiter = ";";
        return false;
    }

    private static bool MatchesAt(string sql, int i, string delimiter) =>
        i + delimiter.Length <= sql.Length &&
        sql.AsSpan(i, delimiter.Length).SequenceEqual(delimiter);

    private static void Add(List<SqlStatement> statements, string sql, int start, int end)
    {
        var text = sql[start..end].Trim();
        if (text.Length > 0)
        {
            statements.Add(new SqlStatement(text, start, end));
        }
    }
}
