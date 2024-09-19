using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryEntityIntraSystemMappingStore : AbstractEntityIntraSystemMappingStore {

  protected readonly Dictionary<EntityIntraSysMap.MappingKey, EntityIntraSysMap> memdb = new();
  
  public override Task<List<EntityIntraSysMap>> GetAll() => Task.FromResult(memdb.Values.ToList());
  public override Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key) => Task.FromResult(memdb[key]);

  public override Task<GetForCoresResult<E>> GetForCores<E>(ICollection<E> cores, SystemName target) {
    var news = new List<(E Core, EntityIntraSysMap.PendingCreate Map)>();
    var updates = new List<(E Core, EntityIntraSysMap.PendingUpdate Map)>();
    cores.ForEach(c => {
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == typeof(E).Name && k.CoreId == c.Id && k.SourceSystem == c.SourceSystem && k.SourceId == c.SourceId && k.TargetSystem == target);
      if (existing == default) news.Add((c, EntityIntraSysMap.Create(c, target)));
      else updates.Add((c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult<E>(news, updates));
  }

  public override Task<List<EntityIntraSysMap.Created>> Create(IEnumerable<EntityIntraSysMap.Created> news) => 
      Task.FromResult(news.Select(map => (EntityIntraSysMap.Created) (memdb[map.Key] = map)).ToList());

  public override Task<List<EntityIntraSysMap.Updated>> Update(IEnumerable<EntityIntraSysMap.Updated> updates) => 
      Task.FromResult(updates.Select(map => (EntityIntraSysMap.Updated) (memdb[map.Key] = map)).ToList());

  public override Task<List<string>> FilterOutBouncedBackIds<C>(SystemName promotingsys, List<string> ids) {
    var bounces = memdb.Values.
      Where(tse => tse.CoreEntity == typeof(C).Name && tse.TargetSystem == promotingsys && ids.Contains(tse.TargetId)).
      Select(tse => tse.TargetId.Value).
      ToList();
    return Task.FromResult(ids.Except(bounces).ToList());
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}