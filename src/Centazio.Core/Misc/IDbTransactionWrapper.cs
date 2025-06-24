namespace Centazio.Core.Misc;

public interface IDbTransactionWrapper : IAsyncDisposable {
  Task Rollback();
}