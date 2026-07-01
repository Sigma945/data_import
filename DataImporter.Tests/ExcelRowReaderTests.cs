using DataImporter.App.Importing;
using DataImporter.App.Importing.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace DataImporter.Tests;

public class ExcelRowReaderTests
{
    private static string WriteWorkbook(string sheetName, Action<ISheet> build)
    {
        var path = Path.Combine(Path.GetTempPath(), $"importtest_{Guid.NewGuid():N}.xlsx");
        using (var wb = new XSSFWorkbook())
        {
            var sheet = wb.CreateSheet(sheetName);
            build(sheet);
            using var fs = File.Create(path);
            wb.Write(fs);
        }
        return path;
    }

    private static ColumnMap Map() => new(new Dictionary<string, string>
    {
        ["Sku"] = "商品編號",
        ["Name"] = "商品名稱",
        ["LaunchDate"] = "上架日期",
    });

    [Fact]
    public void Read_ReturnsRows_WithMappedValues()
    {
        var path = WriteWorkbook("Products", sheet =>
        {
            var header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("商品編號");
            header.CreateCell(1).SetCellValue("商品名稱");
            header.CreateCell(2).SetCellValue("上架日期");

            var r1 = sheet.CreateRow(1);
            r1.CreateCell(0).SetCellValue("SKU-001");
            r1.CreateCell(1).SetCellValue("無線滑鼠");
            r1.CreateCell(2).SetCellValue("2026-01-15");
        });

        try
        {
            var reader = new ExcelRowReader();
            var rows = reader.Read(path, "Products", Map());

            Assert.Single(rows);
            Assert.Equal(2, rows[0].RowNumber);
            Assert.Equal("SKU-001", rows[0].GetString("Sku"));
            Assert.Equal("無線滑鼠", rows[0].GetString("Name"));
            Assert.True(rows[0].TryGetDate("LaunchDate", out var d));
            Assert.Equal(new DateTime(2026, 1, 15), d);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Read_NormalizesNumericDateCells()
    {
        var path = WriteWorkbook("Products", sheet =>
        {
            var header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("商品編號");
            header.CreateCell(1).SetCellValue("商品名稱");
            header.CreateCell(2).SetCellValue("上架日期");

            var wb = sheet.Workbook;
            var dateStyle = wb.CreateCellStyle();
            dateStyle.DataFormat = wb.CreateDataFormat().GetFormat("yyyy-mm-dd");

            var r1 = sheet.CreateRow(1);
            r1.CreateCell(0).SetCellValue("SKU-002");
            r1.CreateCell(1).SetCellValue("鍵盤");
            var dateCell = r1.CreateCell(2);
            dateCell.SetCellValue(new DateTime(2026, 3, 1));
            dateCell.CellStyle = dateStyle;
        });

        try
        {
            var reader = new ExcelRowReader();
            var rows = reader.Read(path, "Products", Map());

            Assert.True(rows[0].TryGetDate("LaunchDate", out var d));
            Assert.Equal(new DateTime(2026, 3, 1), d);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Read_Throws_WhenSheetMissing()
    {
        var path = WriteWorkbook("Products", sheet => sheet.CreateRow(0).CreateCell(0).SetCellValue("商品編號"));
        try
        {
            var reader = new ExcelRowReader();
            Assert.Throws<ImportConfigurationException>(() => reader.Read(path, "NoSuchSheet", Map()));
        }
        finally { File.Delete(path); }
    }
}
