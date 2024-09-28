using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository, ICoreStorageGetter {
  
  public Task<E> Get<E>(CoreEntityType obj, string id) where E : class, ICoreEntity {
    if (!db.ContainsKey(obj) || !db[obj].ContainsKey(id)) throw new Exception($"Core entity [{obj}#{id}] not found");
    return Task.FromResult((E) db[obj][id]);
  }
  
  public Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after) {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.DateCreated > after || c.Value.DateUpdated > after).Select(c => c.Value).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType obj, IList<string> coreids) {
    if (!db.TryGetValue(obj, out var fulllst)) throw new Exception($"Core entity type [{obj}] not found");
    var lst = coreids.Select(id => fulllst.Single(e => e.Value.Id == id).Value).ToList();
    return Task.FromResult(lst);
  }

  public Task<IEnumerable<E>> Query<E>(CoreEntityType obj, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult<IEnumerable<E>>(Array.Empty<E>());
    return Task.FromResult(fulllst.Values.Cast<E>().Where(predicate.Compile()));
  }

  public Task<IEnumerable<E>> Query<E>(CoreEntityType obj, string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<E>(string query)`.  Use `Query<E>(Expression<Func<E, bool>> predicate)` instead.");
}