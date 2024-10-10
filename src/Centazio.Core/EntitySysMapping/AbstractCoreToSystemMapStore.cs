using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<Map.Created>> Create(SystemName system, CoreEntityType coretype, List<Map.Created> maps);
  Task<List<Map.Updated>> Update(SystemName system, CoreEntityType coretype, List<Map.Updated> maps);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> cores);
  Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityType coretype, List<CoreEntityId> coreids);
  Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityType coretype, List<SystemEntityId> sysids);

  /// <summary>
  /// Gets a map from SourceId to the correct CoreId for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSourceIdToCoreIdMap(SystemName system, CoreEntityType coretype, List<ICoreEntity> entities);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<Map.Created>> Create(SystemName system, CoreEntityType coretype, List<Map.Created> creates);
  public abstract Task<List<Map.Updated>> Update(SystemName system, CoreEntityType coretype, List<Map.Updated> updates);
  public abstract Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> cores);
  public abstract Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityType coretype, List<CoreEntityId> coreids);
  
  public abstract Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityType coretype, List<SystemEntityId> sysids);
  public abstract Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSourceIdToCoreIdMap(SystemName system, CoreEntityType coretype, List<ICoreEntity> entities);
  public abstract ValueTask DisposeAsync();

}