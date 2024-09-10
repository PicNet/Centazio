namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageUpserter : ICoreStorageUpserter {

  protected readonly Dictionary<Type, Dictionary<string, ICoreEntity>> db = new();

  public Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity 
    => Task.FromResult(entities.Select(UpsertImpl));

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