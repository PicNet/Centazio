namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<CoreEntityType, Dictionary<string, CoreEntityAndChecksum>> db = new();

  public Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var checksums = new Dictionary<string, string>();
    if (!entities.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(obj, out var dbtype)) return Task.FromResult(checksums);
    var result = entities
        .Where(e => dbtype.ContainsKey(e.Id))
        .Select(e => dbtype[e.Id])
        .ToDictionary(e => e.CoreEntity.Id, e => e.Checksum);
    return Task.FromResult(result);
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<CoreEntityAndChecksum> entities) {
    if (!db.ContainsKey(obj)) db[obj] = new Dictionary<string, CoreEntityAndChecksum>();
    var upserted = entities.Select(e => {
      db[obj][e.CoreEntity.Id] = e;
      return e.CoreEntity;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}