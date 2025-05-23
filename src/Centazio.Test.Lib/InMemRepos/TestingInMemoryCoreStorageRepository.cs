namespace Centazio.Test.Lib.InMemRepos;

public class TestingInMemoryCoreStorageRepository : ITestingCoreStorage {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<ValidString, CoreEntityAndMeta>> db = [];
  
  public Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (!db.TryGetValue(coretype, out var fulllst)) return Task.FromResult(new List<CoreEntityAndMeta>());
    var lst = fulllst
        .Where(c => c.Value.Meta.LastUpdateSystem != exclude && c.Value.Meta.DateCreated > after || c.Value.Meta.DateUpdated > after).Select(c => c.Value)
        .ToList();
    return Task.FromResult(lst);
  }
  
  
  public Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult(new List<CoreEntityAndMeta>());
    if (!db.TryGetValue(coretype, out var fulllst)) throw new Exception("Could not find all specified core entities");
    var lst = coreids.Select(id => fulllst.SingleOrDefault(e => e.Value.CoreEntity.CoreId == id).Value)
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
        .ToDictionary(t => t.CoreEntity.CoreId, e => e.Meta.CoreEntityChecksum);
    return Task.FromResult(result);
  }

  public Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = [];
    var upserted = entities.Select(e => {
      return db[coretype][e.CoreEntity.CoreId] = e;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public Task<List<CoreEntity>> GetAllCoreEntities() {
    if (!db.TryGetValue(CoreEntityTypeName.From<CoreEntity>(), out var fulllst)) return Task.FromResult(new List<CoreEntity>());
    return Task.FromResult(fulllst.Values.Select(ec => ec.As<CoreEntity>()).ToList());
  }
  
  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }
}