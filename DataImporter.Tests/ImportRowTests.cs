using DataImporter.App.Importing;
using Xunit;

namespace DataImporter.Tests;

public class ImportRowTests
{
    private static ImportRow Row(Dictionary<string, string?> values) => new(rowNumber: 2, values);

    [Fact]
    public void GetString_TrimsAndReturnsValue()
    {
        var row = Row(new() { ["Name"] = "  滑鼠  " });
        Assert.Equal("滑鼠", row.GetString("Name"));
    }

    [Fact]
    public void GetString_ReturnsNull_WhenMissingOrBlank()
    {
        var row = Row(new() { ["Name"] = "   " });
        Assert.Null(row.GetString("Name"));
        Assert.Null(row.GetString("NotThere"));
    }

    [Fact]
    public void TryGetDecimal_ParsesNumber()
    {
        var row = Row(new() { ["Price"] = "199.50" });
        Assert.True(row.TryGetDecimal("Price", out var value));
        Assert.Equal(199.50m, value);
    }

    [Fact]
    public void TryGetInt_FailsOnNonInteger()
    {
        var row = Row(new() { ["Stock"] = "abc" });
        Assert.False(row.TryGetInt("Stock", out _));
    }

    [Fact]
    public void TryGetDate_ParsesIsoDate()
    {
        var row = Row(new() { ["LaunchDate"] = "2026-01-15" });
        Assert.True(row.TryGetDate("LaunchDate", out var value));
        Assert.Equal(new DateTime(2026, 1, 15), value);
    }
}
