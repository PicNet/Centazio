using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreToSystemMapping;

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> maps);
  Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> maps);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents);
  Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);

  /// <summary>
  /// Gets a map from `SystemId` to the correct `CoreId` for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSystemIdToCoreIdMap(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);
  Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedEntitySystemIdsFromCoreEntities(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey);
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedEntityCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> creates);
  public abstract Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> updates);
  public abstract Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents);
  public abstract Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids);
  
  public abstract Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);
  public abstract Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSystemIdToCoreIdMap(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);
  
  public async Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedEntitySystemIdsFromCoreEntities(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey) {
    var fks = coreents.Select(e => new CoreEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var maps = await GetExistingMappingsFromCoreIds(system, coretype, fks);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SystemId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {coretype} with CoreIds [{String.Join(",", missing)}]");
    
    return dict;
  }
  public async Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedEntityCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory) {
    var fks = sysents.Select(e => new SystemEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var dict = (await GetExistingMappingsFromSystemIds(system, coretype, fks)).ToDictionary(m => m.SystemId, m => m.CoreId);
    if (!mandatory) return dict;
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntityCoreIdsFromSystemIds[{system}] - Could not find {coretype} with SystemIds [{String.Join(",", missing)}]");
    return dict;
  }
  
  public abstract ValueTask DisposeAsync();

}