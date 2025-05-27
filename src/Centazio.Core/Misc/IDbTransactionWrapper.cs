namespace Centazio.Core.Misc;

public interface IDbTransactionWrapper : IDisposable {
  Task Commit();
  Task Rollback();
}