using System.Data.Common;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public class EfTransactionWrapper<T>(T db, DbTransaction impl) : IDbTransactionWrapper
    where T : DbContext {
  public event EventHandler<EventArgs>? OnCommit;
  public event EventHandler<EventArgs>? OnRollback;
  public event EventHandler<EventArgs>? OnEnd;
  
  public T Db => db;
  
  private bool rolledback;
  private bool committed;
  
  public async ValueTask DisposeAsync() {
    if (committed) throw new Exception("transaction has already committed");
    if (rolledback) return;
    committed = true;
    await impl.CommitAsync();
    await db.DisposeAsync();
    
    OnCommit?.Invoke(this, EventArgs.Empty);
    OnEnd?.Invoke(this, EventArgs.Empty);
  }
  
  public async Task Rollback() {
    if (committed) throw new Exception("transaction has already committed");
    if (rolledback) throw new Exception("transaction has already rolledback");
    rolledback = true;
    await impl.RollbackAsync();
    
    OnRollback?.Invoke(this, EventArgs.Empty);
    OnEnd?.Invoke(this, EventArgs.Empty);
  }
}