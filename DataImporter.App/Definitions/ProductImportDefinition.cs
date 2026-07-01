using System.Linq.Expressions;
using DataImporter.App.Importing;
using DataImporter.Model.Entities;

namespace DataImporter.App.Definitions;

/// <summary>Product 的匯入規則：以 Sku 為業務鍵，含必填/型別/範圍驗證。</summary>
public class ProductImportDefinition : IImportDefinition<Product>
{
    public Expression<Func<Product, string>> KeySelector => p => p.Sku;

    public IEnumerable<ImportError> Validate(ImportRow row)
    {
        if (row.GetString("Sku") is null)
            yield return new ImportError(row.RowNumber, "Sku", "商品編號必填");

        if (row.GetString("Name") is null)
            yield return new ImportError(row.RowNumber, "Name", "商品名稱必填");

        if (!row.TryGetDecimal("Price", out var price) || price < 0)
            yield return new ImportError(row.RowNumber, "Price", "售價需為非負數字");

        if (row.GetString("Stock") is not null && (!row.TryGetInt("Stock", out var stock) || stock < 0))
            yield return new ImportError(row.RowNumber, "Stock", "庫存需為非負整數");

        if (row.GetString("LaunchDate") is not null && !row.TryGetDate("LaunchDate", out _))
            yield return new ImportError(row.RowNumber, "LaunchDate", "上架日期格式無法解析");
    }

    public Product Map(ImportRow row)
    {
        var now = DateTime.UtcNow;
        return new Product
        {
            Sku = row.GetString("Sku")!,
            Name = row.GetString("Name")!,
            Price = row.TryGetDecimal("Price", out var price) ? price : 0m,
            Stock = row.TryGetInt("Stock", out var stock) ? stock : 0,
            Category = row.GetString("Category"),
            LaunchDate = row.TryGetDate("LaunchDate", out var d) ? d : null,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Apply(Product target, Product source)
    {
        target.Name = source.Name;
        target.Price = source.Price;
        target.Stock = source.Stock;
        target.Category = source.Category;
        target.LaunchDate = source.LaunchDate;
        target.UpdatedAt = DateTime.UtcNow;
    }
}
