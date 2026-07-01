using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DataImporter.App.Importing.Excel;

/// <summary>用 NPOI 讀取 .xlsx，依 ColumnMap 把資料列轉成 ImportRow。</summary>
public class ExcelRowReader
{
    public IReadOnlyList<ImportRow> Read(string filePath, string sheetName, ColumnMap columnMap)
    {
        if (!File.Exists(filePath))
        {
            throw new ImportConfigurationException($"找不到匯入檔案：{filePath}");
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheet = workbook.GetSheet(sheetName)
            ?? throw new ImportConfigurationException($"找不到工作表：{sheetName}");

        var headerRow = sheet.GetRow(sheet.FirstRowNum)
            ?? throw new ImportConfigurationException("Excel 沒有標題列。");

        var headerCells = new List<string?>();
        for (var c = 0; c < headerRow.LastCellNum; c++)
        {
            headerCells.Add(headerRow.GetCell(c)?.ToString());
        }

        var columns = columnMap.ResolveColumns(headerCells);

        var rows = new List<ImportRow>();
        for (var r = sheet.FirstRowNum + 1; r <= sheet.LastRowNum; r++)
        {
            var dataRow = sheet.GetRow(r);
            if (dataRow is null || IsEmptyRow(dataRow))
            {
                continue;
            }

            var values = new Dictionary<string, string?>();
            foreach (var (logical, colIndex) in columns)
            {
                values[logical] = ReadCell(dataRow.GetCell(colIndex));
            }

            rows.Add(new ImportRow(r + 1, values)); // r 為 0-based，列號顯示用 1-based
        }

        return rows;
    }

    private static bool IsEmptyRow(IRow row)
    {
        if (row.LastCellNum < 0)
        {
            return true;
        }

        for (var c = row.FirstCellNum; c < row.LastCellNum; c++)
        {
            var cell = row.GetCell(c);
            if (cell is not null && !string.IsNullOrWhiteSpace(cell.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    private static string? ReadCell(ICell? cell)
    {
        if (cell is null)
        {
            return null;
        }

        return cell.CellType switch
        {
            CellType.Numeric when DateUtil.IsCellDateFormatted(cell)
                => cell.DateCellValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
            CellType.Boolean => cell.BooleanCellValue ? "1" : "0",
            CellType.Formula => cell.ToString(),
            _ => cell.ToString(),
        };
    }
}
