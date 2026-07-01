using DataImporter.App.Definitions;
using DataImporter.App.Importing;
using DataImporter.App.Importing.Excel;
using DataImporter.App.Sample;
using DataImporter.Model;
using DataImporter.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// 子命令：產生範例 Excel。用法：dotnet run -- make-sample
if (args.Length > 0 && args[0] == "make-sample")
{
    var sampleOptions = config.GetSection("Import").Get<ImportOptions>()!;
    // 路徑相對於 AppContext.BaseDirectory（輸出目錄），以便開發時用 dotnet run 直接寫到輸出目錄
    var sampleFilePath = Path.IsPathRooted(sampleOptions.File)
        ? sampleOptions.File
        : Path.Combine(AppContext.BaseDirectory, sampleOptions.File);
    SampleDataGenerator.Write(sampleFilePath);
    Console.WriteLine($"✅ 已產生範例檔：{sampleFilePath}");
    return 0;
}

var importOptions = config.GetSection("Import").Get<ImportOptions>()
    ?? throw new InvalidOperationException("缺少 Import 設定。");

// 解析匯入檔案的完整路徑（相對路徑以輸出目錄為基準）
var filePath = Path.IsPathRooted(importOptions.File)
    ? importOptions.File
    : Path.Combine(AppContext.BaseDirectory, importOptions.File);

try
{
    using var db = CreateContext(config);

    // Demo 使用 SQLite，啟動時自動建表。換成真實已存在的 SQL Server 時可移除這行。
    db.Database.EnsureCreated();

    var reader = new ExcelRowReader();
    var rows = reader.Read(filePath, importOptions.Sheet, importOptions.ToColumnMap());

    var engine = new ImportEngine<Product>(db, new ProductImportDefinition());
    var result = await engine.RunAsync(rows, CancellationToken.None);

    PrintReport(result);
    return result.HasErrors ? 2 : 0;
}
catch (ImportConfigurationException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ 設定錯誤：{ex.Message}");
    Console.ResetColor();
    return 1;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ 匯入失敗：{ex.Message}");
    Console.ResetColor();
    return 1;
}

static AppDbContext CreateContext(IConfiguration config)
{
    var provider = config["Provider"] ?? "Sqlite";
    var builder = new DbContextOptionsBuilder<AppDbContext>();

    switch (provider)
    {
        case "Sqlite":
            var sqlite = config.GetConnectionString("Sqlite")!;
            var dbPath = sqlite.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
            var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            builder.UseSqlite(sqlite);
            break;
        case "SqlServer":
            builder.UseSqlServer(config.GetConnectionString("SqlServer")!);
            break;
        default:
            throw new ImportConfigurationException($"未知的 Provider：{provider}（可用 Sqlite 或 SqlServer）");
    }

    return new AppDbContext(builder.Options);
}

static void PrintReport(ImportResult result)
{
    if (result.Errors.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n⚠️  {result.Errors.Count} 筆驗證錯誤（已略過）：");
        foreach (var e in result.Errors)
        {
            Console.WriteLine($"   - {e}");
        }
        Console.ResetColor();
    }

    Console.WriteLine("\n=== 匯入摘要 ===");
    Console.WriteLine($"新增：{result.Inserted}");
    Console.WriteLine($"更新：{result.Updated}");
    Console.WriteLine($"略過：{result.Skipped}");
    Console.WriteLine($"耗時：{result.Elapsed.TotalMilliseconds:0} ms");
}
