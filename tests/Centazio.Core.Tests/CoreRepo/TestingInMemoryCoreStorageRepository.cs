using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository, ICoreStorageGetter {
  
  public Task<E> Get<E>(ObjectName obj, string id) where E : class, ICoreEntity {
    if (!db.ContainsKey(obj) || !db[obj].ContainsKey(id)) throw new Exception($"Core entity [{obj}#{id}] not found");
    return Task.FromResult((E) db[obj][id]);
  }
  
  public Task<List<ICoreEntity>> Get(ObjectName obj, DateTime after) {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.DateCreated > after || c.Value.DateUpdated > after).Select(c => c.Value).ToList();
    return Task.FromResult(lst);
  }
  
  public Task<IEnumerable<E>> Query<E>(ObjectName obj, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult<IEnumerable<E>>(Array.Empty<E>());
    return Task.FromResult(fulllst.Values.Cast<E>().Where(predicate.Compile()));
  }

  public Task<IEnumerable<E>> Query<E>(ObjectName obj, string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<T>(string query)`.  Use `Query<T>(Expression<Func<T, bool>> predicate)` instead.");
}