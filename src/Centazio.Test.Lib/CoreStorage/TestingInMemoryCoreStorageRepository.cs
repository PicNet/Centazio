using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public class TestingInMemoryCoreStorageRepository : InMemoryCoreStorageUpserter, ICoreStorageWithQuery {
  
  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.CoreEntity.LastUpdateSystem != exclude.Value && c.Value.CoreEntity.DateCreated > after || c.Value.CoreEntity.DateUpdated > after).Select(c => c.Value.CoreEntity).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult(new List<ICoreEntity>());
    if (!db.TryGetValue(coretype, out var fulllst)) throw new Exception("Could not find all specified core entities");
    var lst = coreids.Select(id => fulllst.SingleOrDefault(e => e.Value.CoreEntity.CoreId == id).Value.CoreEntity)
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(e => e is not null)
        .ToList();
    if (lst.Count != coreids.Count) throw new Exception("Could not find all specified core entities");
    return Task.FromResult(lst);
  }

  public Task<List<E>> Query<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<E>());
    var compiled = predicate.Compile();
    return Task.FromResult(fulllst.Values.Select(ec => ec.CoreEntity.To<E>()).Where(compiled).ToList());
  }

  public Task<List<E>> Query<E>(CoreEntityTypeName coretype, string query) where E : class, ICoreEntity => 
      throw new NotSupportedException("InMemoryCoreStorageRepository does not support `Query<E>(string query)`.  Use `Query<E>(Expression<Func<E, bool>> predicate)` instead.");
  
  public Dictionary<CoreEntityTypeName, Dictionary<ValidString, (ICoreEntity CoreEntity, CoreEntityChecksum CoreEntityChecksum)>> MemDb => db; 
}