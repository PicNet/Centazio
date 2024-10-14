using Centazio.Core.Checksum;

namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<CoreEntityTypeName, Dictionary<ValidString, Containers.CoreChecksum>> db = new();

  public Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var checksums = new Dictionary<CoreEntityId, CoreEntityChecksum>();
    if (!coreids.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(coretype, out var dbtype)) return Task.FromResult(checksums);
    var result = coreids
        .Where(coreid => dbtype.ContainsKey(coreid))
        .Select(coreid => dbtype[coreid])
        .ToDictionary(e => e.Core.CoreId, e => new CoreEntityChecksum(e.CoreEntityChecksum));
    return Task.FromResult(result);
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<Containers.CoreChecksum> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new Dictionary<ValidString, Containers.CoreChecksum>();
    var upserted = entities.Select(e => {
      db[coretype][e.Core.CoreId] = e;
      return e.Core;
    }).ToList();
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}