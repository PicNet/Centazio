using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public interface IEntityIntraSystemMappingStore : IAsyncDisposable {
  
  Task<CoreToExternalMap.Created> Create(CoreToExternalMap.Created create);
  Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> maps);
  
  Task<CoreToExternalMap.Updated> Update(CoreToExternalMap.Updated map);
  Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> maps);
  
  Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key);
  
  // todo: can this be removed and replaced with more explicit methods
  // such as ones below (FindTargetIds)
  Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  Task<List<CoreToExternalMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids);
  
  /// <summary>
  /// Gets the CoreId for a specific `CoreEntityType` given its `TargetId` and `TargetSystem`.
  /// </summary>
  Task<string?> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);

}

public abstract class AbstractEntityIntraSystemMappingStore : IEntityIntraSystemMappingStore {
  
  public async Task<CoreToExternalMap.Created> Create(CoreToExternalMap.Created create) => (await Create([create])).Single();
  public abstract Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> creates);
  
  public async Task<CoreToExternalMap.Updated> Update(CoreToExternalMap.Updated update) => (await Update([update])).Single();
  public abstract Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> updates);
  
  public abstract Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key);
  public abstract Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  public abstract Task<List<CoreToExternalMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids);
  public abstract Task<List<CoreToExternalMap>> GetAll();
  
  public abstract Task<string?> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);
  public abstract ValueTask DisposeAsync();

}