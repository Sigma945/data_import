using DataImporter.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataImporter.Tests.TestSupport;

/// <summary>建立一個以 SQLite in-memory 為後端的 AppDbContext；連線在 Dispose 前保持開啟。</summary>
public sealed class SqliteTestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public SqliteTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>另開一個共用同一 in-memory 資料庫的新 context（驗證持久化用）。</summary>
    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
