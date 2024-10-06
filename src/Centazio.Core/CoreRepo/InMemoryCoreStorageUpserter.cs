namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<CoreEntityType, Dictionary<string, Containers.CoreChecksum>> db = new();

  public Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var checksums = new Dictionary<string, string>();
    if (!entities.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(obj, out var dbtype)) return Task.FromResult(checksums);
    var result = entities
        .Where(e => dbtype.ContainsKey(e.Id))
        .Select(e => dbtype[e.Id])
        .ToDictionary(e => e.Core.Id, e => e.Checksum);
    return Task.FromResult(result);
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<Containers.CoreChecksum> entities) {
    if (!db.ContainsKey(obj)) db[obj] = new Dictionary<string, Containers.CoreChecksum>();
    var upserted = entities.Select(e => {
      db[obj][e.Core.Id] = e;
      return e.Core;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}