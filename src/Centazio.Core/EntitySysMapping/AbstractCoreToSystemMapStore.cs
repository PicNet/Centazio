using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public interface ICoreToSystemMapStore : IAsyncDisposable {
  
  Task<CoreToExternalMap.Created> Create(CoreToExternalMap.Created create);
  Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> maps);
  
  Task<CoreToExternalMap.Updated> Update(CoreToExternalMap.Updated map);
  Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> maps);
  
  Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key);
  
  Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external);
  Task<List<CoreToExternalMap>> GetForCores(CoreEntityType coretype, List<string> coreids, SystemName external);
  
  /// <summary>
  /// Gets the CoreId for a specific `CoreEntityType` given its `ExternalId` and `ExternalSystem`.
  /// This methods throws an exception if the expected CoreEntity is not found.
  /// </summary>
  Task<string> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {
  
  public async Task<CoreToExternalMap.Created> Create(CoreToExternalMap.Created create) => (await Create([create])).Single();
  public abstract Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> creates);
  
  public async Task<CoreToExternalMap.Updated> Update(CoreToExternalMap.Updated update) => (await Update([update])).Single();
  public abstract Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> updates);
  
  public abstract Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key);
  public abstract Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external);
  public abstract Task<List<CoreToExternalMap>> GetForCores(CoreEntityType coretype, List<string> coreids, SystemName external);
  public abstract Task<List<CoreToExternalMap>> GetAll();
  
  public abstract Task<string> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);
  public abstract ValueTask DisposeAsync();

}