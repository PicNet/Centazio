using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore.Storage;

namespace Centazio.Providers.EF;

public class EfTransactionWrapper(IDbContextTransaction impl, Action onend) : IDbTransactionWrapper, IAsyncDisposable {

  public void Dispose() {
    onend();
    impl.Dispose();
  }
  
  public async ValueTask DisposeAsync() {
    onend();
    await impl.DisposeAsync(); 
  }
  
  public async Task Commit() {
    onend();
    await impl.CommitAsync();
  }
  
  public async Task Rollback() {
    onend();
    await impl.RollbackAsync();
  }

}