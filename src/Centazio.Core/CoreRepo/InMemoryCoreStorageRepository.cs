using System.Linq.Expressions;

namespace Centazio.Core.CoreRepo;

public class InMemoryCoreStorageRepository : ICoreStorageRepository {

  private readonly Dictionary<Type, Dictionary<string, ICoreEntity>> db = new();
  
  public Task<T> Get<T>(string id) where T : ICoreEntity {
    if (!db.ContainsKey(typeof(T)) || !db[typeof(T)].ContainsKey(id)) throw new Exception($"Core entity [{typeof(T).Name}#{id}] not found");
    return Task.FromResult((T)db[typeof(T)][id]);
  }

  public Task<T> Upsert<T>(T e) where T : ICoreEntity {
    if (!db.ContainsKey(e.GetType())) db[e.GetType()] = new Dictionary<string, ICoreEntity>();
    
    db[e.GetType()][e.Id] = e;
    return Task.FromResult(e);
  }

  public async Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    return await Task.WhenAll(entities.Select(Upsert));
  }

  public Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity {
    if (!db.ContainsKey(typeof(T))) throw new Exception($"Core entity type [{typeof(T).Name}] not found");
    return Task.FromResult(db[typeof(T)].Values.Cast<T>().Where(predicate.Compile()));
  }

  public void Dispose() => db.Clear();

}