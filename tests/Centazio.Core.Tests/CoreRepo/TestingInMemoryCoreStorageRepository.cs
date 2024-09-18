using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository, ICoreStorageGetter {
  
  public Task<E> Get<E>(string id) where E : class, ICoreEntity {
    if (!db.ContainsKey(typeof(E)) || !db[typeof(E)].ContainsKey(id)) throw new Exception($"Core entity [{typeof(E).Name}#{id}] not found");
    return Task.FromResult((E)db[typeof(E)][id]);
  }
  
  public Task<List<E>> Get<E>(DateTime after) where E : ICoreEntity {
    if (!db.ContainsKey(typeof(E))) return Task.FromResult(new List<E>());
    var lst = db[typeof(E)].Where(c => c.Value.DateCreated > after || c.Value.DateUpdated > after).Select(c => c.Value).Cast<E>().ToList();
    return Task.FromResult(lst);
  }
  
  public Task<IEnumerable<E>> Query<E>(Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.ContainsKey(typeof(E))) return Task.FromResult<IEnumerable<E>>(Array.Empty<E>());
    return Task.FromResult(db[typeof(E)].Values.Cast<E>().Where(predicate.Compile()));
  }

  public Task<IEnumerable<E>> Query<E>(string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<T>(string query)`.  Use `Query<T>(Expression<Func<T, bool>> predicate)` instead.");
}