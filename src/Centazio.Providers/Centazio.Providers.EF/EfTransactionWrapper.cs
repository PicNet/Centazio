using System.Data.Common;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public class EfTransactionWrapper<T>(T db, DbTransaction impl, Action onend) : IDbTransactionWrapper, IAsyncDisposable
    where T : DbContext {
  public T Db => db;
  
  public void Dispose() {
    db.Dispose();
    impl.Dispose();
    onend();
  }
  
  public async ValueTask DisposeAsync() {
    await db.DisposeAsync();
    await impl.DisposeAsync();
    onend();
  }
  
  public async Task Commit() {
    await impl.CommitAsync();
    onend();
  }
  
  public async Task Rollback() {
    await impl.RollbackAsync();
    onend();
  }
}