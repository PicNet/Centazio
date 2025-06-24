using System.Data.Common;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

// todo GT: onend should be an Event
public class EfTransactionWrapper<T>(T db, DbTransaction impl, Action onend) : IDbTransactionWrapper
    where T : DbContext {
  public T Db => db;
  
  private bool rolledback;
  private bool committed;
  
  public async ValueTask DisposeAsync() {
    if (committed) throw new Exception("transaction has already committed");
    if (rolledback) return;
    committed = true;
    await impl.CommitAsync();
    await db.DisposeAsync();
    onend();
  }
  
  public async Task Rollback() {
    if (committed) throw new Exception("transaction has already committed");
    if (rolledback) throw new Exception("transaction has already rolledback");
    rolledback = true;
    await impl.RollbackAsync();
    onend();
  }
}