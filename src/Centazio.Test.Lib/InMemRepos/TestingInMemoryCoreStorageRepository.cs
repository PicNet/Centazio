using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.InMemRepos;

public class TestingInMemoryCoreStorageRepository : ITestingCoreStorage {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<ValidString, (CoreEntityAndMeta CoreEntityAndMeta, CoreEntityChecksum CoreEntityChecksum)>> db = new();
  
  public Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<CoreEntityAndMeta>());
    var lst = fulllst
        .Where(c => c.Value.CoreEntityAndMeta.Meta.LastUpdateSystem != exclude.Value && c.Value.CoreEntityAndMeta.Meta.DateCreated > after || c.Value.CoreEntityAndMeta.Meta.DateUpdated > after).Select(c => c.Value.CoreEntityAndMeta)
        .ToList();
    return Task.FromResult(lst);
  }
  
  
  public Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult(new List<CoreEntityAndMeta>());
    if (!db.TryGetValue(coretype, out var fulllst)) throw new Exception("Could not find all specified core entities");
    var lst = coreids.Select(id => fulllst.SingleOrDefault(e => e.Value.CoreEntityAndMeta.CoreEntity.CoreId == id).Value.CoreEntityAndMeta)
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
        .ToDictionary(t => t.CoreEntityAndMeta.CoreEntity.CoreId, e => new CoreEntityChecksum(e.CoreEntityChecksum));
    return Task.FromResult(result);
  }

  public Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<(CoreEntityAndMeta UpdatedCoreEntityAndMeta, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new Dictionary<ValidString, (CoreEntityAndMeta CoreEntity, CoreEntityChecksum CoreEntityChecksum)>();
    var upserted = entities.Select(e => {
      db[coretype][e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId] = (e.UpdatedCoreEntityAndMeta, e.UpdatedCoreEntityChecksum);
      return e.UpdatedCoreEntityAndMeta;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public Task<List<CoreEntity>> GetAllCoreEntities() {
    if (!db.TryGetValue(CoreEntityTypeName.From<CoreEntity>(), out var fulllst)) return Task.FromResult(new List<CoreEntity>());
    return Task.FromResult(fulllst.Values.Select(ec => ec.CoreEntityAndMeta.As<CoreEntity>()).ToList());
  }
  
  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }
}