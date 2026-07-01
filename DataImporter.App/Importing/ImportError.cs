namespace DataImporter.App.Importing;

/// <summary>單一驗證錯誤：第幾列、哪個欄位、錯誤訊息。</summary>
/// <param name="Row">Excel 列號（1-based，含標題列；資料第一列通常為 2）。</param>
/// <param name="Column">邏輯欄位名稱（對應 appsettings 的 Import:Mapping 鍵）。</param>
/// <param name="Message">錯誤描述。</param>
public readonly record struct ImportError(int Row, string Column, string Message)
{
    public override string ToString() => $"第 {Row} 列 [{Column}]：{Message}";
}
