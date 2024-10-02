using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> maps);
  Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> maps);
  
  Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external);
  Task<List<CoreToExternalMap>> GetForCores(CoreEntityType coretype, List<string> coreids, SystemName external);
  
  /// <summary>
  /// Gets the CoreId for a specific `CoreEntityType` given its `ExternalId` and `ExternalSystem`.
  /// This methods returns null if the entity is not found.
  /// </summary>
  Task<string?> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);

  /// <summary>
  /// Gets a map from SourceId to the correct CoreId for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<string, string>> GetPreExistingCoreIds(List<ICoreEntity> duplicates, SystemName system);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> creates);
  public abstract Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> updates);
  public abstract Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external);
  public abstract Task<List<CoreToExternalMap>> GetForCores(CoreEntityType coretype, List<string> coreids, SystemName external);
  public abstract Task<List<CoreToExternalMap>> GetAll();
  
  public abstract Task<string?> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys);
  public abstract Task<Dictionary<string, string>> GetPreExistingCoreIds(List<ICoreEntity> duplicates, SystemName system);
  public abstract ValueTask DisposeAsync();

}