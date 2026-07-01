using System.Linq.Expressions;
using DataImporter.App.Importing;
using DataImporter.Model;
using DataImporter.Model.Entities;
using DataImporter.Tests.TestSupport;
using Xunit;

namespace DataImporter.Tests;

public class ImportEngineTests
{
    // 簡化版的 Product 定義：Sku 必填、Price >= 0。
    private sealed class TestProductDefinition : IImportDefinition<Product>
    {
        public Expression<Func<Product, string>> KeySelector => p => p.Sku;

        public IEnumerable<ImportError> Validate(ImportRow row)
        {
            if (row.GetString("Sku") is null)
                yield return new ImportError(row.RowNumber, "Sku", "Sku 必填");
            if (!row.TryGetDecimal("Price", out var price) || price < 0)
                yield return new ImportError(row.RowNumber, "Price", "Price 需為非負數");
        }

        public Product Map(ImportRow row) => new()
        {
            Sku = row.GetString("Sku")!,
            Name = row.GetString("Name") ?? string.Empty,
            Price = row.TryGetDecimal("Price", out var p) ? p : 0m,
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 1),
        };

        public void Apply(Product target, Product source)
        {
            target.Name = source.Name;
            target.Price = source.Price;
            target.UpdatedAt = new DateTime(2026, 2, 2);
        }
    }

    private static ImportRow Row(int n, string? sku, string? name, string? price)
        => new(n, new Dictionary<string, string?> { ["Sku"] = sku, ["Name"] = name, ["Price"] = price });

    private static ImportEngine<Product> Engine(AppDbContext ctx)
        => new(ctx, new TestProductDefinition());

    [Fact]
    public async Task RunAsync_InsertsValidRows_AndSkipsInvalid()
    {
        using var db = new SqliteTestContext();
        var rows = new[]
        {
            Row(2, "SKU-1", "滑鼠", "100"),
            Row(3, null, "壞資料", "100"),   // Sku 缺 → 略過
            Row(4, "SKU-2", "鍵盤", "-5"),   // Price 負 → 略過
        };

        var result = await Engine(db.Context).RunAsync(rows, CancellationToken.None);

        Assert.Equal(1, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, result.Errors.Count);

        using var verify = db.NewContext();
        Assert.Equal(1, verify.Products.Count());
        Assert.Equal("滑鼠", verify.Products.Single().Name);
    }

    [Fact]
    public async Task RunAsync_UpdatesExistingRow_ByBusinessKey()
    {
        using var db = new SqliteTestContext();
        db.Context.Products.Add(new Product { Sku = "SKU-1", Name = "舊名", Price = 50, CreatedAt = new DateTime(2025, 1, 1), UpdatedAt = new DateTime(2025, 1, 1) });
        await db.Context.SaveChangesAsync();

        var rows = new[] { Row(2, "SKU-1", "新名", "80") };
        var result = await Engine(db.Context).RunAsync(rows, CancellationToken.None);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Updated);

        using var verify = db.NewContext();
        var p = verify.Products.Single();
        Assert.Equal("新名", p.Name);
        Assert.Equal(80m, p.Price);
        Assert.Equal(new DateTime(2025, 1, 1), p.CreatedAt); // CreatedAt 不變
    }
}
