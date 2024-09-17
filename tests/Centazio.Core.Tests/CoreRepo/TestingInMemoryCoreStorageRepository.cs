using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository {
  
  public Task<C> Get<C>(string id) where C : class, ICoreEntity {
    if (!db.ContainsKey(typeof(C)) || !db[typeof(C)].ContainsKey(id)) throw new Exception($"Core entity [{typeof(C).Name}#{id}] not found");
    return Task.FromResult((C)db[typeof(C)][id]);
  }
  
  public Task<IEnumerable<C>> Query<C>(Expression<Func<C, bool>> predicate) where C : class, ICoreEntity {
    if (!db.ContainsKey(typeof(C))) return Task.FromResult<IEnumerable<C>>(Array.Empty<C>());
    return Task.FromResult(db[typeof(C)].Values.Cast<C>().Where(predicate.Compile()));
  }

  public Task<IEnumerable<C>> Query<C>(string query) where C : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<T>(string query)`.  Use `Query<T>(Expression<Func<T, bool>> predicate)` instead.");
  
}