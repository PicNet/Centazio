using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public interface IEntityIntraSystemMappingStore : IAsyncDisposable {
  
  Task<EntityIntraSysMap.Created> Create(EntityIntraSysMap.Created create);
  Task<List<EntityIntraSysMap.Created>> Create(List<EntityIntraSysMap.Created> maps);
  
  Task<EntityIntraSysMap.Updated> Update(EntityIntraSysMap.Updated map);
  Task<List<EntityIntraSysMap.Updated>> Update(List<EntityIntraSysMap.Updated> maps);
  
  Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key);
  
  // todo: can this be removed and replaced with more explicit methods
  // such as ones below (FindTargetIds)
  Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids);
  
  /// <summary>
  /// Gets the CoreId for a specific `CoreEntityType` given its `TargetId` and `TargetSystem`.
  /// </summary>
  Task<string?> GetCoreIdForTargetSys(CoreEntityType obj, string targetid, SystemName targetsys);

}

public abstract class AbstractEntityIntraSystemMappingStore : IEntityIntraSystemMappingStore {
  
  public async Task<EntityIntraSysMap.Created> Create(EntityIntraSysMap.Created create) => (await Create([create])).Single();
  public abstract Task<List<EntityIntraSysMap.Created>> Create(List<EntityIntraSysMap.Created> creates);
  
  public async Task<EntityIntraSysMap.Updated> Update(EntityIntraSysMap.Updated update) => (await Update([update])).Single();
  public abstract Task<List<EntityIntraSysMap.Updated>> Update(List<EntityIntraSysMap.Updated> updates);
  
  public abstract Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key);
  public abstract Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  public abstract Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids);
  public abstract Task<List<EntityIntraSysMap>> GetAll();
  
  public abstract Task<string?> GetCoreIdForTargetSys(CoreEntityType obj, string targetid, SystemName targetsys);
  public abstract ValueTask DisposeAsync();

}