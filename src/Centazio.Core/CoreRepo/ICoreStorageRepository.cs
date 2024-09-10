using System.Linq.Expressions;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity {
  public string SourceSystem { get; }
  public string Id { get; }
  public DateTime DateCreated { get; }
  public DateTime DateUpdated { get; }
  public DateTime SourceSystemDateUpdated { get; }
} 

public interface ICoreStorageRepository : IAsyncDisposable {
  Task<T> Get<T>(string id) where T : ICoreEntity;
  Task<T> Upsert<T>(T e) where T : ICoreEntity;
  Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity;
  Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity;
  Task<IEnumerable<T>> Query<T>(string query) where T : ICoreEntity;
  
}