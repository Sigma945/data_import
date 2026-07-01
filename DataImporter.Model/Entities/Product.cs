namespace DataImporter.Model.Entities;

/// <summary>示範用商品實體；以 <see cref="Sku"/> 作為匯入的業務鍵。</summary>
public class Product
{
    public int ProductId { get; set; }

    /// <summary>商品編號（業務鍵，唯一）。</summary>
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? Category { get; set; }

    public DateTime? LaunchDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
