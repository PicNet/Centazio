namespace Centazio.Core.CoreRepo;

public interface ICoreEntity {
  public string SourceSystem { get; }
  public string Id { get; }
  public DateTime DateCreated { get; }
  public DateTime DateUpdated { get; }
  public DateTime SourceSystemDateUpdated { get; }
} 

public interface ICoreStorageUpserter : IAsyncDisposable {
  Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity;
}
