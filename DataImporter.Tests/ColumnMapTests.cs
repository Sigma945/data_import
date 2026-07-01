using DataImporter.App.Importing;
using Xunit;

namespace DataImporter.Tests;

public class ColumnMapTests
{
    private static ColumnMap CreateMap() => new(new Dictionary<string, string>
    {
        ["Sku"] = "商品編號",
        ["Name"] = "商品名稱",
    });

    [Fact]
    public void ResolveColumns_MapsLogicalFieldsToHeaderIndexes()
    {
        var map = CreateMap();
        var headers = new string?[] { "商品名稱", "商品編號" }; // 故意調換順序

        var result = map.ResolveColumns(headers);

        Assert.Equal(1, result["Sku"]);
        Assert.Equal(0, result["Name"]);
    }

    [Fact]
    public void ResolveColumns_Throws_WhenHeaderMissing()
    {
        var map = CreateMap();
        var headers = new string?[] { "商品名稱" }; // 缺「商品編號」

        var ex = Assert.Throws<ImportConfigurationException>(() => map.ResolveColumns(headers));
        Assert.Contains("商品編號", ex.Message);
    }
}
