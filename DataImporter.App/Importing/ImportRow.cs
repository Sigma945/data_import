using System.Globalization;

namespace DataImporter.App.Importing;

/// <summary>一列已正規化為字串的資料，提供型別安全取值。</summary>
public class ImportRow
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    public ImportRow(int rowNumber, IReadOnlyDictionary<string, string?> values)
    {
        RowNumber = rowNumber;
        _values = values;
    }

    /// <summary>Excel 列號（1-based）。</summary>
    public int RowNumber { get; }

    /// <summary>取得去頭尾空白後的字串；空白或不存在回傳 null。</summary>
    public string? GetString(string field)
    {
        if (_values.TryGetValue(field, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            return raw.Trim();
        }
        return null;
    }

    public bool TryGetDecimal(string field, out decimal value)
    {
        value = 0m;
        var s = GetString(field);
        return s is not null && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public bool TryGetInt(string field, out int value)
    {
        value = 0;
        var s = GetString(field);
        return s is not null && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    public bool TryGetDate(string field, out DateTime value)
    {
        value = default;
        var s = GetString(field);
        return s is not null && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
