using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DataImporter.App.Importing;

/// <summary>
/// 通用匯入引擎：逐列驗證、在單一交易內依業務鍵 upsert，最後產生摘要。
/// 驗證失敗的列會被略過並記錄錯誤，有效列照常匯入。
/// </summary>
public class ImportEngine<TEntity> where TEntity : class
{
    private readonly DbContext _db;
    private readonly IImportDefinition<TEntity> _definition;
    private readonly Func<TEntity, string> _keyFunc;

    public ImportEngine(DbContext db, IImportDefinition<TEntity> definition)
    {
        _db = db;
        _definition = definition;
        _keyFunc = definition.KeySelector.Compile();
    }

    public async Task<ImportResult> RunAsync(IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ImportResult();

        // 1. 驗證 + 對應
        var mapped = new List<(string Key, TEntity Entity)>();
        foreach (var row in rows)
        {
            var errors = _definition.Validate(row).ToList();
            if (errors.Count > 0)
            {
                result.Errors.AddRange(errors);
                result.Skipped++;
                continue;
            }

            var entity = _definition.Map(row);
            mapped.Add((_keyFunc(entity), entity));
        }

        if (mapped.Count == 0)
        {
            stopwatch.Stop();
            result.Elapsed = stopwatch.Elapsed;
            return result;
        }

        var set = _db.Set<TEntity>();

        // 2. 交易內 upsert
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var keys = mapped.Select(m => m.Key).ToList();
            var existing = (await set.Where(BuildKeyInPredicate(keys)).ToListAsync(ct))
                .ToDictionary(_keyFunc);

            foreach (var (key, entity) in mapped)
            {
                if (existing.TryGetValue(key, out var current))
                {
                    _definition.Apply(current, entity);
                    result.Updated++;
                }
                else
                {
                    set.Add(entity);
                    result.Inserted++;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        stopwatch.Stop();
        result.Elapsed = stopwatch.Elapsed;
        return result;
    }

    /// <summary>建立 e =&gt; keys.Contains(keySelector(e)) 的述詞，讓 EF 翻成 SQL IN。</summary>
    private Expression<Func<TEntity, bool>> BuildKeyInPredicate(List<string> keys)
    {
        var selector = _definition.KeySelector;
        var containsCall = Expression.Call(
            Expression.Constant(keys),
            typeof(List<string>).GetMethod(nameof(List<string>.Contains))!,
            selector.Body);
        return Expression.Lambda<Func<TEntity, bool>>(containsCall, selector.Parameters);
    }
}
