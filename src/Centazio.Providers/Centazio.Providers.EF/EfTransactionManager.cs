using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Centazio.Providers.EF;

public class EfTransactionManager<T>(Func<T> getdb) : IAsyncDisposable 
    where T : DbContext {
  private EfTransactionWrapper<T>? transaction;
  
  public async Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) {
    if (transaction is not null) throw new Exception($"transaction is already active in this ef repository/context.  `reuse` is only supported accross different repositories/contexts.");
    if (reuse is not null) { return reuse; }
    
    var db = getdb();
    return transaction = new EfTransactionWrapper<T>(
        db, (await db.Database.BeginTransactionAsync()).GetDbTransaction(), () => transaction = null);
  }
  
  public async Task<R> UseDb<R>(Func<T, Task<R>> func) {
    // if using a transaction do not dispose it after usage, let the transaction (EfTransactionWrapper)
    //    handle disposing of the Context and internal Transaction
    if (transaction is not null) { return await func(transaction.Db); }

    await using var db = getdb();
    return await func(db);
  }

  public ValueTask DisposeAsync() => transaction?.DisposeAsync() ?? ValueTask.CompletedTask;
}