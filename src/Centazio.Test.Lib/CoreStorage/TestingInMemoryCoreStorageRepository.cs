using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository, ICoreStorage {
  
  public Task<E> Get<E>(CoreEntityType obj, ValidString id) where E : class, ICoreEntity {
    if (!db.ContainsKey(obj) || !db[obj].ContainsKey(id)) throw new Exception($"Core entity [{obj}({id})] not found");
    return Task.FromResult(db[obj][id].Core.To<E>());
  }
  
  public Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after, SystemName exclude) {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.Core.LastUpdateSystem != exclude.Value && c.Value.Core.DateCreated > after || c.Value.Core.DateUpdated > after).Select(c => c.Value.Core).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType obj, List<ValidString> coreids) {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = coreids.Select(id => fulllst.Single(e => e.Value.Core.Id == id).Value.Core).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<E>> Query<E>(CoreEntityType obj, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(obj, out var fulllst)) return Task.FromResult(new List<E>());
    var compiled = predicate.Compile();
    return Task.FromResult(fulllst.Values.Select(ec => ec.Core.To<E>()).Where(compiled).ToList());
  }

  public Task<List<E>> Query<E>(CoreEntityType obj, string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<E>(string query)`.  Use `Query<E>(Expression<Func<E, bool>> predicate)` instead.");
  
  public Dictionary<CoreEntityType, Dictionary<ValidString, Containers.CoreChecksum>> MemDb => db; 
}