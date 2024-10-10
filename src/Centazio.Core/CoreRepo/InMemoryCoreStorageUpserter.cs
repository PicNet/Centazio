using Centazio.Core.Checksum;

namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<CoreEntityType, Dictionary<ValidString, Containers.CoreChecksum>> db = new();

  public Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityType coretype, List<ICoreEntity> entities) {
    var checksums = new Dictionary<CoreEntityId, CoreEntityChecksum>();
    if (!entities.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(coretype, out var dbtype)) return Task.FromResult(checksums);
    var result = entities
        .Where(e => dbtype.ContainsKey(e.Id))
        .Select(e => dbtype[e.Id])
        .ToDictionary(e => e.Core.Id, e => new CoreEntityChecksum(e.CoreEntityChecksum));
    return Task.FromResult(result);
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityType coretype, List<Containers.CoreChecksum> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new Dictionary<ValidString, Containers.CoreChecksum>();
    var upserted = entities.Select(e => {
      db[coretype][e.Core.Id] = e;
      return e.Core;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}