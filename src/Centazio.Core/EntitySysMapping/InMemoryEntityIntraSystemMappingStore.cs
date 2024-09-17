using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryEntityIntraSystemMappingStore : AbstractEntityIntraSystemMappingStore {

  protected readonly Dictionary<EntityIntraSystemMapping.MappingKey, EntityIntraSystemMapping> memdb = new();
  
  public override Task<List<EntityIntraSystemMapping>> GetAll() => Task.FromResult(memdb.Values.ToList());
  
  public override Task<List<(C Core, EntityIntraSystemMapping Map)>> Get<C>(ICollection<C> cores, SystemName target) => Task.FromResult(cores.Select(c => {
    var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == typeof(C).Name && k.CoreId == c.Id && k.SourceSystem == c.SourceSystem && k.SourceId == c.SourceId && k.TargetSystem == target);
    return existing == default ? 
        (Core: c, Map: EntityIntraSystemMapping.CreatePending(c, target)) : 
        (Core: c, Map: memdb[existing]);
  }).ToList());
  
  public override Task<IEnumerable<EntityIntraSystemMapping>> Create(IEnumerable<CreateEntityIntraSystemMapping> news) => 
      Task.FromResult(news.Select(n => {
        var map = EntityIntraSystemMapping.Create(n);
        return memdb[map.Key] = map;
      }));

  public override Task<IEnumerable<EntityIntraSystemMapping>> Update(IEnumerable<UpdateEntityIntraSystemMapping> updates) => 
      Task.FromResult(updates.Select(update => {
        var map = memdb[update.Key];
        return memdb[update.Key] = update.Status == EEntityMappingStatus.Success ? map.Success() : map.Error(update.Error);
      }));

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