using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryEntityIntraSystemMappingStore : AbstractEntityIntraSystemMappingStore {

  protected readonly Dictionary<EntityIntraSysMap.MappingKey, EntityIntraSysMap> memdb = new();
  public override Task<List<EntityIntraSysMap>> GetAll() => Task.FromResult(memdb.Values.ToList());
  public override Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key) => Task.FromResult(memdb[key]);

  public override Task<GetForCoresResult> GetForCores(ICollection<ICoreEntity> cores, SystemName target, CoreEntityType obj) {
    var news = new List<CoreAndPendingCreateMap>();
    var updates = new List<CoreAndPendingUpdateMap>();
    cores.ForEach(c => {
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.SourceSystem == c.SourceSystem && k.SourceId == c.SourceId && k.TargetSystem == target);
      if (existing == default) news.Add(new CoreAndPendingCreateMap(c, EntityIntraSysMap.Create(c, target, obj)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult(news, updates));
  }
  
  public override Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName source, SystemName target, ICollection<string> coreids) => Task.FromResult(coreids.Select(cid => {
    Console.WriteLine($"FindTargetIds [Core:{coretype}, Source:{source} Target: {target}] CoreIds[{String.Join(',', coreids)}]");
    var key = memdb.Keys.Single(k => k.CoreEntity == coretype && k.CoreId == cid && k.SourceSystem == source && k.TargetSystem == target);
    return memdb[key];
  }).ToList());

  public override Task<List<EntityIntraSysMap.Created>> Create(ICollection<EntityIntraSysMap.Created> news) {
    Console.WriteLine($"Creating EntityIntraSysMaps [{news.Count}] - {String.Join(',', news.Select(n => n.Key))}");
    return Task.FromResult(news.Select(map => (EntityIntraSysMap.Created)(memdb[map.Key] = map)).ToList());
  }

  public override Task<List<EntityIntraSysMap.Updated>> Update(ICollection<EntityIntraSysMap.Updated> updates) {
    return Task.FromResult(updates.Select(map => (EntityIntraSysMap.Updated)(memdb[map.Key] = map)).ToList());
  }

  public override Task<List<string>> FilterOutBouncedBackIds(SystemName promotingsys, CoreEntityType obj, List<string> ids) {
    var bounces = memdb.Values.
      Where(tse => tse.CoreEntity == obj && tse.TargetSystem == promotingsys && ids.Contains(tse.TargetId)).
      Select(tse => tse.TargetId.Value).
      ToList();
    return Task.FromResult(ids.Except(bounces).ToList());
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}