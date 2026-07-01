namespace DataImporter.App.Importing;

/// <summary>邏輯欄位名稱 → Excel 標題文字 的對應（來自 appsettings 的 Import:Mapping）。</summary>
public class ColumnMap
{
    private readonly IReadOnlyDictionary<string, string> _logicalToHeader;

    public ColumnMap(IReadOnlyDictionary<string, string> logicalToHeader)
    {
        ArgumentNullException.ThrowIfNull(logicalToHeader);
        _logicalToHeader = logicalToHeader;
    }

    public IReadOnlyCollection<string> LogicalFields => _logicalToHeader.Keys.ToArray();

    /// <summary>依標題列文字，解析出每個邏輯欄位對應的欄索引（0-based）。</summary>
    /// <exception cref="ImportConfigurationException">當某個設定的標題在標題列找不到時。</exception>
    public IReadOnlyDictionary<string, int> ResolveColumns(IReadOnlyList<string?> headerCells)
    {
        var headerIndex = new Dictionary<string, int>();
        for (var i = 0; i < headerCells.Count; i++)
        {
            var text = headerCells[i]?.Trim();
            if (!string.IsNullOrEmpty(text) && !headerIndex.ContainsKey(text))
            {
                headerIndex[text] = i;
            }
        }

        var result = new Dictionary<string, int>();
        var missing = new List<string>();
        foreach (var (logical, header) in _logicalToHeader)
        {
            if (headerIndex.TryGetValue(header, out var idx))
            {
                result[logical] = idx;
            }
            else
            {
                missing.Add(header);
            }
        }

        if (missing.Count > 0)
        {
            throw new ImportConfigurationException(
                $"Excel 標題列缺少對應欄位：{string.Join("、", missing)}");
        }

        return result;
    }
}
