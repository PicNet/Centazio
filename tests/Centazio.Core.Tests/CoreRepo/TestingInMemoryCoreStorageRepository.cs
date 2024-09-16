using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository {
  
  public Task<T> Get<T>(string id) where T : class, ICoreEntity {
    if (!db.ContainsKey(typeof(T)) || !db[typeof(T)].ContainsKey(id)) throw new Exception($"Core entity [{typeof(T).Name}#{id}] not found");
    return Task.FromResult((T)db[typeof(T)][id]);
  }
  
  public Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : class, ICoreEntity {
    if (!db.ContainsKey(typeof(T))) return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
    return Task.FromResult(db[typeof(T)].Values.Cast<T>().Where(predicate.Compile()));
  }

  public Task<IEnumerable<T>> Query<T>(string query) where T : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<T>(string query)`.  Use `Query<T>(Expression<Func<T, bool>> predicate)` instead.");
  
}