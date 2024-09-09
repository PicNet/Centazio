using System.Linq.Expressions;

namespace Centazio.Core.CoreRepo;

public interface ICoreEntity {
  public string SourceSystem { get; }
  public string Id { get; }
  public DateTime DateCreated { get; }
  public DateTime DateUpdated { get; }
  public DateTime LastSourceSystemUpdate { get; }
} 

public interface ICoreStorageRepository {
  T Get<T>(string id) where T : ICoreEntity;
  T Upsert<T>(T e) where T : ICoreEntity;
  IEnumerable<T> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity; 
}

public class InMemoryCoreStorageRepository : ICoreStorageRepository {

  private readonly Dictionary<Type, Dictionary<string, ICoreEntity>> db = new();
  
  public T Get<T>(string id) where T : ICoreEntity => (T) db[typeof(T)][id];
  public T Upsert<T>(T e) where T : ICoreEntity {
    if (!db.ContainsKey(typeof(T))) db[typeof(T)] = new Dictionary<string, ICoreEntity>();
    
    db[typeof(T)][e.Id] = e;
    return e;
  }
  
  public IEnumerable<T> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity => 
      db[typeof(T)].Values.Cast<T>().Where(predicate.Compile());

}