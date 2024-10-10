using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageRepository, ICoreStorage {
  
  public Task<E> Get<E>(CoreEntityType coretype, CoreEntityId coreid) where E : class, ICoreEntity {
    if (!db.ContainsKey(coretype) || !db[coretype].ContainsKey(coreid)) throw new Exception($"Core entity [{coretype}({coreid})] not found");
    return Task.FromResult(db[coretype][coreid].Core.To<E>());
  }
  
  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityType coretype, DateTime after) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.Core.LastUpdateSystem != exclude.Value && c.Value.Core.DateCreated > after || c.Value.Core.DateUpdated > after).Select(c => c.Value.Core).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType coretype, List<CoreEntityId> coreids) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = coreids.Select(id => fulllst.Single(e => e.Value.Core.CoreId == id).Value.Core).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<E>> Query<E>(CoreEntityType coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<E>());
    var compiled = predicate.Compile();
    return Task.FromResult(fulllst.Values.Select(ec => ec.Core.To<E>()).Where(compiled).ToList());
  }

  public Task<List<E>> Query<E>(CoreEntityType coretype, string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<E>(string query)`.  Use `Query<E>(Expression<Func<E, bool>> predicate)` instead.");
  
  public Dictionary<CoreEntityType, Dictionary<ValidString, Containers.CoreChecksum>> MemDb => db; 
}