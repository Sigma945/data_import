namespace DataImporter.App.Importing;

/// <summary>對應 appsettings.json 的 "Import" 區塊。</summary>
public class ImportOptions
{
    public string File { get; set; } = string.Empty;
    public string Sheet { get; set; } = string.Empty;
    public Dictionary<string, string> Mapping { get; set; } = new();

    public ColumnMap ToColumnMap() => new(Mapping);
}
