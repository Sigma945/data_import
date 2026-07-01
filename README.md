# DataImporter

通用、可重用的 C# 資料匯入工具：從 Excel(.xlsx) 讀取資料，經過驗證後以交易方式 upsert 進資料庫。內建一個以 SQLite 為後端、零安裝即可執行的 Product 匯入示範。

## 功能

- **設定驅動的欄位對應**：Excel 標題 ↔ 實體欄位的對應寫在 `appsettings.json`，不寫死。
- **逐列資料驗證 + 錯誤報告**：必填、型別、範圍檢查；輸出「第幾列／哪一欄／什麼錯」。
- **交易式 Upsert**：依業務鍵（示範用 `Sku`）判斷新增或更新，整批包在交易內，失敗回滾。
- **匯入摘要**：新增 / 更新 / 略過筆數與耗時，並以行程結束碼回報（0 成功、2 有驗證錯誤、1 例外）。

## 架構

```
DataImporter.Model   EF Core 實體 + AppDbContext
DataImporter.App     Console 程式
  Importing/         可重用匯入引擎（讀取、驗證、upsert、摘要）
  Definitions/       各資料表的匯入規則（實作 IImportDefinition）
DataImporter.Tests   xUnit 測試（SQLite in-memory）
```

匯入流程：`ExcelRowReader` 讀檔 → 逐列 `IImportDefinition.Validate`/`Map` → `ImportEngine` 在交易內依業務鍵 upsert → 輸出 `ImportResult`。

## 執行示範

需求：.NET 8 SDK。

```bash
# 產生範例 Excel（已內含於原始碼，通常不需重跑）
dotnet run --project DataImporter.App -- make-sample

# 執行匯入（使用 SQLite，自動建立 data/demo.db）
dotnet run --project DataImporter.App
```

範例檔故意放了兩筆錯誤列（缺商品編號、負售價），可看到驗證錯誤報告與略過計數；再執行一次，有效資料會全部變成「更新」，展示 upsert。

## 跑測試

```bash
dotnet test
```

## 換成你自己的資料庫

換一張新資料表，只需要動 **設定 + 一次 scaffold + 一個 Definition + Program.cs 一行**；`Importing/` 底下的通用引擎（讀 Excel、驗證、交易、upsert、摘要、錯誤報告）完全不用改。

以下以「匯入客戶 `Customer`」為例。

### 1. 改 `appsettings.json`

```jsonc
{
  "Provider": "SqlServer",                    // 由 "Sqlite" 改成 "SqlServer"
  "ConnectionStrings": {
    "Sqlite": "Data Source=data/demo.db",     // demo 用，可留著
    "SqlServer": "Data Source=你的伺服器;Database=你的DB;User ID=帳號;Password=密碼;TrustServerCertificate=true"
  },
  "Import": {
    "File": "ImportFile/customers.xlsx",       // 你的 Excel 檔名
    "Sheet": "客戶清單",                        // 你的工作表名稱
    "Mapping": {                               // 鍵 = 程式裡的邏輯欄位名, 值 = Excel 標題列文字（需一字不差）
      "CustomerNo": "客戶編號",
      "Name": "客戶名稱",
      "Phone": "電話",
      "Email": "Email"
    }
  }
}
```

### 2. Scaffold 你的資料表（產生 entity）

```bash
dotnet ef dbcontext scaffold "<你的 SqlServer 連線字串>" Microsoft.EntityFrameworkCore.SqlServer \
  -c AppDbContext -o Entities --project DataImporter.Model --force
```

- 保留 `-c AppDbContext`，名稱固定（`Program.cs` 與引擎都依賴它）。
- `--force` 會覆蓋 demo 的 `Product.cs`（或手動刪掉 demo entity）。

### 3. 新增 `Definitions/CustomerImportDefinition.cs`

仿照 `ProductImportDefinition`，實作 `IImportDefinition<Customer>` 的四個成員：

```csharp
using System.Linq.Expressions;
using DataImporter.App.Importing;
using DataImporter.Model.Entities;   // scaffold 後 entity 的命名空間

namespace DataImporter.App.Definitions;

public class CustomerImportDefinition : IImportDefinition<Customer>
{
    // 業務鍵：upsert 用哪一欄判斷「同一筆」
    public Expression<Func<Customer, string>> KeySelector => c => c.CustomerNo;

    // 逐列驗證：回傳所有錯誤
    public IEnumerable<ImportError> Validate(ImportRow row)
    {
        if (row.GetString("CustomerNo") is null)
            yield return new ImportError(row.RowNumber, "CustomerNo", "客戶編號必填");
        if (row.GetString("Name") is null)
            yield return new ImportError(row.RowNumber, "Name", "客戶名稱必填");
        // 需要的話再加 Email 格式、電話等驗證
    }

    // 一列 → 新 entity
    public Customer Map(ImportRow row) => new()
    {
        CustomerNo = row.GetString("CustomerNo")!,
        Name       = row.GetString("Name")!,
        Phone      = row.GetString("Phone"),
        Email      = row.GetString("Email"),
    };

    // upsert 命中既有資料時，要覆蓋哪些欄位（保留 PK、建立時間等）
    public void Apply(Customer target, Customer source)
    {
        target.Name  = source.Name;
        target.Phone = source.Phone;
        target.Email = source.Email;
    }
}
```

取值用 `ImportRow` 的型別安全方法：`GetString` / `TryGetDecimal` / `TryGetInt` / `TryGetDate`。只有在 `Mapping` 設定過的鍵才取得到值。

### 4. 在 `Program.cs` 換型別

```csharp
var engine = new ImportEngine<Customer>(db, new CustomerImportDefinition());
```

若**目標資料庫已存在**，刪掉這行（它只是 demo 用 SQLite 自動建表）：

```csharp
db.Database.EnsureCreated();
```

### 5. 放 Excel 檔並執行

把檔案放到 `DataImporter.App/ImportFile/`（檔名與 `Import:File` 一致），然後：

```bash
dotnet run --project DataImporter.App
```
