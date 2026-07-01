using System.Linq.Expressions;

namespace DataImporter.App.Importing;

/// <summary>
/// 描述某個實體型別的匯入規則：欄位對應、驗證、轉換、業務鍵與更新行為。
/// 要匯入新資料表時，實作此介面即可重用 <see cref="ImportEngine{TEntity}"/>。
/// </summary>
public interface IImportDefinition<TEntity> where TEntity : class
{
    /// <summary>業務鍵選擇器，用於 upsert 比對（例如 p =&gt; p.Sku）。</summary>
    Expression<Func<TEntity, string>> KeySelector { get; }

    /// <summary>逐列驗證；回傳所有錯誤（無錯則為空集合）。</summary>
    IEnumerable<ImportError> Validate(ImportRow row);

    /// <summary>把一列資料轉成新的實體（呼叫前該列已通過驗證）。</summary>
    TEntity Map(ImportRow row);

    /// <summary>upsert 命中既有資料時，把新值套用到既有實體（保留 PK、CreatedAt 等）。</summary>
    void Apply(TEntity target, TEntity source);
}
