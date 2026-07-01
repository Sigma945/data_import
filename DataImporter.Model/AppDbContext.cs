using DataImporter.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataImporter.Model;

/// <summary>
/// 應用程式 DbContext。
/// 換成自己的資料庫時，可用
/// <c>dotnet ef dbcontext scaffold "&lt;連線字串&gt;" Microsoft.EntityFrameworkCore.SqlServer -c AppDbContext -o Entities</c>
/// 重新產生實體與 context（記得保留 -c AppDbContext 讓名稱一致）。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.ProductId);
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Sku).HasMaxLength(50).IsRequired();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.Category).HasMaxLength(100);
        });
    }
}
