using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.CoreToSystemMapping;

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> maps);
  Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> maps);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents);
  Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);

  /// <summary>
  /// Gets a map from `SystemId` to the correct `CoreId` for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSystemIdToCoreIdMap(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> creates);
  public abstract Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> updates);
  public abstract Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents);
  public abstract Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  
  public abstract Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);
  public abstract Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSystemIdToCoreIdMap(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);
  public abstract ValueTask DisposeAsync();

}