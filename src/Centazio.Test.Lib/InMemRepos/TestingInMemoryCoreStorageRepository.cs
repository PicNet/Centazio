using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.InMemRepos;

public class TestingInMemoryCoreStorageRepository : ITestingCoreStorage {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<ValidString, (ICoreEntity CoreEntity, CoreEntityChecksum CoreEntityChecksum)>> db = new();
  
  public Task<List<ICoreEntity>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<ICoreEntity>());
    var lst = fulllst.Where(c => c.Value.CoreEntity.LastUpdateSystem != exclude.Value && c.Value.CoreEntity.DateCreated > after || c.Value.CoreEntity.DateUpdated > after).Select(c => c.Value.CoreEntity).ToList();
    return Task.FromResult(lst);
  }

  public Task<List<ICoreEntity>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult(new List<ICoreEntity>());
    if (!db.TryGetValue(coretype, out var fulllst)) throw new Exception("Could not find all specified core entities");
    var lst = coreids.Select(id => fulllst.SingleOrDefault(e => e.Value.CoreEntity.CoreId == id).Value.CoreEntity)
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(e => e is not null)
        .ToList();
    if (lst.Count != coreids.Count) throw new Exception("Could not find all specified core entities");
    return Task.FromResult(lst);
  }
  
  public Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var checksums = new Dictionary<CoreEntityId, CoreEntityChecksum>();
    if (!coreids.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(coretype, out var dbtype)) return Task.FromResult(checksums);
    var result = coreids
        .Where(coreid => dbtype.ContainsKey(coreid))
        .Select(coreid => dbtype[coreid])
        .ToDictionary(t => t.CoreEntity.CoreId, e => new CoreEntityChecksum(e.CoreEntityChecksum));
    return Task.FromResult(result);
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new Dictionary<ValidString, (ICoreEntity CoreEntity, CoreEntityChecksum CoreEntityChecksum)>();
    var upserted = entities.Select(e => {
      db[coretype][e.UpdatedCoreEntity.CoreId] = (e.UpdatedCoreEntity, e.UpdatedCoreEntityChecksum);
      return e.UpdatedCoreEntity;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public Task<List<CoreEntity>> GetAllCoreEntities() => GetAll<CoreEntity>(CoreEntityTypeName.From<CoreEntity>(), e => true);
  
  // todo: do we need this method (replace with GetAllCoreEntities) 
  public Task<List<E>> GetAll<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<E>());
    var compiled = predicate.Compile();
    return Task.FromResult(fulllst.Values.Select(ec => ec.CoreEntity.To<E>()).Where(compiled).ToList());
  }
  
  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }
}