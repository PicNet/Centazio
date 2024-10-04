using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public record WriteEntitiesToTargetSystem(List<CoreAndPendingCreateMap> Created, List<CoreExternalMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}


public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<CoreToExternalMap.Created>> Create(CoreEntityType coretype, SystemName system, List<CoreToExternalMap.Created> maps);
  Task<List<CoreToExternalMap.Updated>> Update(CoreEntityType coretype, SystemName system, List<CoreToExternalMap.Updated> maps);
  
  Task<GetForCoresResult> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName external);
  Task<List<CoreToExternalMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName external);
  Task<List<CoreToExternalMap>> GetExistingMappingsFromExternalIds(CoreEntityType coretype, List<string> externalids, SystemName external);

  /// <summary>
  /// Gets a map from SourceId to the correct CoreId for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<CoreToExternalMap.Created>> Create(CoreEntityType coretype, SystemName system, List<CoreToExternalMap.Created> creates);
  public abstract Task<List<CoreToExternalMap.Updated>> Update(CoreEntityType coretype, SystemName system, List<CoreToExternalMap.Updated> updates);
  public abstract Task<GetForCoresResult> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName external);
  public abstract Task<List<CoreToExternalMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName external);
  
  public abstract Task<List<CoreToExternalMap>> GetExistingMappingsFromExternalIds(CoreEntityType coretype, List<string> externalids, SystemName external);
  public abstract Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system);
  public abstract ValueTask DisposeAsync();

}