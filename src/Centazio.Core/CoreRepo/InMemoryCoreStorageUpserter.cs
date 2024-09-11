namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<Type, Dictionary<string, ICoreEntity>> db = new();

  public Task<Dictionary<string, string>> GetChecksums<T>(List<T> entities) where T : ICoreEntity {
    var checksums = new Dictionary<string, string>();
    if (!entities.Any()) return Task.FromResult(checksums);
    if (!db.TryGetValue(entities.First().GetType(), out var dbtype)) return Task.FromResult(checksums);
    
    return Task.FromResult(entities
        .Where(e => dbtype.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => dbtype[e.Id].Checksum));
  }

  public Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    var upserted = entities.Select(UpsertImpl).ToList();
    return Task.FromResult((IEnumerable<T>) upserted);
  }

  private T UpsertImpl<T>(T e) where T : ICoreEntity {
    if (!db.ContainsKey(e.GetType())) db[e.GetType()] = new Dictionary<string, ICoreEntity>();
    db[e.GetType()][e.Id] = e;
    return e;
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}