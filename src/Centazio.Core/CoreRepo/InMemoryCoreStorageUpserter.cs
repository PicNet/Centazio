namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<CoreEntityType, Dictionary<string, ICoreEntity>> db = new();

  public Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var checksums = new Dictionary<string, string>();
    if (!entities.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(obj, out var dbtype)) return Task.FromResult(checksums);
    
    return Task.FromResult(entities
        .Where(e => dbtype.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => dbtype[e.Id].Checksum));
  }

  public Task<IEnumerable<ICoreEntity>> Upsert(CoreEntityType obj, IEnumerable<ICoreEntity> entities) {
    if (!db.ContainsKey(obj)) db[obj] = new Dictionary<string, ICoreEntity>();
    var upserted = entities.Select(e => db[obj][e.Id] = e).ToList();
    return Task.FromResult<IEnumerable<ICoreEntity>>(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}