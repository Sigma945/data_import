namespace DataImporter.App.Importing;

/// <summary>設定錯誤（例如 Excel 缺少對應的標題欄）時拋出。</summary>
public class ImportConfigurationException : Exception
{
    public ImportConfigurationException(string message) : base(message) { }
}
