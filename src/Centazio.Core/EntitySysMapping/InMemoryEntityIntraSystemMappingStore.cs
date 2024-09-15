using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryEntityIntraSystemMappingStore : AbstractEntityIntraSystemMappingStore {

  protected readonly Dictionary<EntityIntraSystemMapping.MappingKey, EntityIntraSystemMapping> saved = new();
  
  public override Task<List<EntityIntraSystemMapping>> Get() => Task.FromResult(saved.Values.ToList());
  
  public override Task<IEnumerable<EntityIntraSystemMapping>> Create(IEnumerable<CreateEntityIntraSystemMapping> news) => 
      Task.FromResult(news.Select(n => {
        var map = EntityIntraSystemMapping.Create(n);
        return saved[map.Key] = map;
      }));

  public override Task<IEnumerable<EntityIntraSystemMapping>> Update(IEnumerable<UpdateEntityIntraSystemMapping> updates) => 
      Task.FromResult(updates.Select(update => {
        var map = saved[update.Key];
        return saved[update.Key] = update.Status == EEntityMappingStatus.Success ? map.Success() : map.Error(update.Error);
      }));

  public override Task<List<string>> FilterOutBouncedBackIds<C>(SystemName promotingsys, List<string> ids) {
    var bounces = saved.Values.
      Where(tse => tse.CoreEntity == typeof(C).Name && tse.TargetSystem == promotingsys && ids.Contains(tse.TargetId)).
      Select(tse => tse.TargetId.Value).
      ToList();
    return Task.FromResult(ids.Except(bounces).ToList());
  }

  public override ValueTask DisposeAsync() { 
    saved.Clear();
    return ValueTask.CompletedTask;
  }
}