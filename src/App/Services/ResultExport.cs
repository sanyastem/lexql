using System.Globalization;
using System.Text;
using System.Text.Json;
using Lexql.Core.Results;

namespace Lexql.App.Services;

public static class ResultExport
{
    public static string ToCsv(ResultGridModel grid)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", grid.Columns.Select(c => CsvField(c.Name))));

        foreach (var row in grid.Rows)
        {
            builder.AppendLine(string.Join(",", row.Select(CsvValue)));
        }

        return builder.ToString();
    }

    public static string ToJson(ResultGridModel grid)
    {
        var records = grid.Rows.Select(row =>
        {
            var record = new Dictionary<string, object?>(grid.Columns.Count);
            for (var i = 0; i < grid.Columns.Count; i++)
            {
                record[grid.Columns[i].Name] = i < row.Length ? row[i] : null;
            }

            return record;
        });

        return JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string CsvValue(object? value) =>
        value switch
        {
            null => string.Empty,
            IFormattable formattable => CsvField(formattable.ToString(null, CultureInfo.InvariantCulture)),
            _ => CsvField(value.ToString() ?? string.Empty),
        };

    private static string CsvField(string text)
    {
        if (text.IndexOfAny(['"', ',', '\n', '\r']) < 0)
        {
            return text;
        }

        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
