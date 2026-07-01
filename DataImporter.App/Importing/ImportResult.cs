namespace DataImporter.App.Importing;

/// <summary>匯入結果摘要。</summary>
public class ImportResult
{
    public int Inserted { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public List<ImportError> Errors { get; } = new();

    public TimeSpan Elapsed { get; set; }

    public bool HasErrors => Errors.Count > 0;
}