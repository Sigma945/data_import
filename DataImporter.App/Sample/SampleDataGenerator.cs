using NPOI.XSSF.UserModel;

namespace DataImporter.App.Sample;

/// <summary>產生示範用的 products.xlsx（含正確列與故意的錯誤列）。</summary>
public static class SampleDataGenerator
{
    public static void Write(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Products");

        var header = sheet.CreateRow(0);
        string[] headers = { "商品編號", "商品名稱", "售價", "庫存", "分類", "上架日期" };
        for (var i = 0; i < headers.Length; i++)
        {
            header.CreateCell(i).SetCellValue(headers[i]);
        }

        // {Sku, Name, Price, Stock, Category, LaunchDate}
        object?[][] data =
        {
            new object?[] { "SKU-001", "無線滑鼠", 199, 50, "周邊", "2026-01-15" },
            new object?[] { "SKU-002", "機械鍵盤", 1290, 20, "周邊", "2026-02-01" },
            new object?[] { "SKU-003", "27吋螢幕", 6990, 8, "顯示器", "2026-03-10" },
            new object?[] { null,      "缺編號商品", 100, 5, "其他", "2026-01-20" }, // 錯誤：缺 Sku
            new object?[] { "SKU-005", "負價商品", -50, 3, "其他", "2026-01-25" },   // 錯誤：負價
        };

        for (var r = 0; r < data.Length; r++)
        {
            var row = sheet.CreateRow(r + 1);
            var d = data[r];
            if (d[0] is string sku) row.CreateCell(0).SetCellValue(sku);
            row.CreateCell(1).SetCellValue((string)d[1]!);
            row.CreateCell(2).SetCellValue(Convert.ToDouble(d[2]));
            row.CreateCell(3).SetCellValue(Convert.ToDouble(d[3]));
            row.CreateCell(4).SetCellValue((string)d[4]!);
            row.CreateCell(5).SetCellValue((string)d[5]!);
        }

        using var fs = File.Create(filePath);
        wb.Write(fs);
    }
}
