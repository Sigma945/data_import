using DataImporter.App.Definitions;
using DataImporter.App.Importing;
using Xunit;

namespace DataImporter.Tests;

public class ProductImportDefinitionTests
{
    private static ImportRow Row(Dictionary<string, string?> v) => new(2, v);

    private static Dictionary<string, string?> Valid() => new()
    {
        ["Sku"] = "SKU-1",
        ["Name"] = "滑鼠",
        ["Price"] = "199",
        ["Stock"] = "10",
        ["Category"] = "周邊",
        ["LaunchDate"] = "2026-01-15",
    };

    [Fact]
    public void Validate_PassesForValidRow()
    {
        var def = new ProductImportDefinition();
        Assert.Empty(def.Validate(Row(Valid())));
    }

    [Fact]
    public void Validate_ReportsMissingSkuAndName()
    {
        var def = new ProductImportDefinition();
        var v = Valid();
        v["Sku"] = null;
        v["Name"] = "  ";

        var errors = def.Validate(Row(v)).ToList();

        Assert.Contains(errors, e => e.Column == "Sku");
        Assert.Contains(errors, e => e.Column == "Name");
    }

    [Fact]
    public void Validate_ReportsNegativePriceAndBadStock()
    {
        var def = new ProductImportDefinition();
        var v = Valid();
        v["Price"] = "-1";
        v["Stock"] = "abc";

        var errors = def.Validate(Row(v)).ToList();

        Assert.Contains(errors, e => e.Column == "Price");
        Assert.Contains(errors, e => e.Column == "Stock");
    }

    [Fact]
    public void Map_BuildsProductWithParsedValues()
    {
        var def = new ProductImportDefinition();
        var p = def.Map(Row(Valid()));

        Assert.Equal("SKU-1", p.Sku);
        Assert.Equal("滑鼠", p.Name);
        Assert.Equal(199m, p.Price);
        Assert.Equal(10, p.Stock);
        Assert.Equal("周邊", p.Category);
        Assert.Equal(new DateTime(2026, 1, 15), p.LaunchDate);
    }

    [Fact]
    public void Apply_UpdatesMutableFields_ButKeepsCreatedAt()
    {
        var def = new ProductImportDefinition();
        var target = new Model.Entities.Product { Sku = "SKU-1", Name = "舊", Price = 1, CreatedAt = new DateTime(2025, 1, 1) };
        var source = def.Map(Row(Valid()));

        def.Apply(target, source);

        Assert.Equal("滑鼠", target.Name);
        Assert.Equal(199m, target.Price);
        Assert.Equal(new DateTime(2025, 1, 1), target.CreatedAt);
    }
}
