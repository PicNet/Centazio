using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Misc;
using Centazio.Core.Write;

namespace Centazio.E2E.Tests.Systems;

public class FunctionHelpers(
    SystemName system,
    IChecksumAlgorithm checksum,
    ICoreToSystemMapStore intra) {
  
  public (List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>) CovertCoreEntitiesToSystemEntitties<E>(
      List<CoreAndPendingCreateMap> tocreate, 
      List<CoreAndPendingUpdateMap> toupdate,
      Func<string, E, ISystemEntity> FromCore) where E : ICoreEntity {
    return (
      tocreate.Select(m => {
        var sysent = FromCore(String.Empty, m.CoreEntity.To<E>()); 
        return m.AddSystemEntity(sysent);
      }).ToList(),
      toupdate.Select(m => {
        var sysent = FromCore(m.Map.SystemId, m.CoreEntity.To<E>());
        TestEntityHasChanges(sysent, m.Map.SystemEntityChecksum);
        return m.AddSystemEntity(sysent);
      }).ToList());
  }
  
  /// <summary>
  /// This method compares the checksum of the entity to be updated with the checksum in the database (Mapping table).
  /// We originally tried to compare the checksum with the state of the entity in the target system, however this is not
  /// valid as the same change can be made in both the source and target system causing this check to fail. 
  /// </summary>
  private void TestEntityHasChanges(ISystemEntity sysent, SystemEntityChecksum syschksm) {
    if (syschksm != checksum.Checksum(sysent)) return;
    
    throw new Exception($"TestEntityHasChanges[{system}/{sysent.GetType().Name}] - No changes found:" +
      $"\n\tExisting Checksum:[{syschksm}]" +
      $"\n\tUpdated[{sysent}]\n\tChecksum Subset[{sysent.GetChecksumSubset()}]" +
      $"\n\tChecksum[{checksum.Checksum(sysent)}]");
  }
  
  public async Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedEntitySystemIdsFromCoreIds(CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey) {
    var fks = coreents.Select(e => new CoreEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var maps = await intra.GetExistingMappingsFromCoreIds(system, coretype, fks);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SystemId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {coretype} with CoreIds [{String.Join(",", missing)}]");
    
    return dict;
  } 
 
  public async Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedEntityCoreIdsFromSystemIds(CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory) {
    var fks = sysents.Select(e => new SystemEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var dict = (await intra.GetExistingMappingsFromSystemIds(system, coretype, fks)).ToDictionary(m => m.SystemId, m => m.CoreId);
    if (!mandatory) return dict;
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntityCoreIdsFromSystemIds[{system}] - Could not find {coretype} with SystemIds [{String.Join(",", missing)}]");
    return dict;
  } 
  
  public SuccessWriteOperationResult GetSuccessWriteOperationResult(List<CoreSystemAndPendingCreateMap> tocreate, IEnumerable<ISystemEntity> createdsysents, List<CoreSystemAndPendingUpdateMap> toupdate, IEnumerable<ISystemEntity> updatedsysents) {
    // todo: this is a bit dodgy, its expecting the created/updated entities to come back in the same order as pre change entities
    return new SuccessWriteOperationResult(
          createdsysents.Select((sysent, idx) => tocreate[idx].Map.SuccessCreate(sysent.SystemId, checksum.Checksum(sysent))).ToList(), 
          updatedsysents.Select((sysent, idx) => toupdate[idx].Map.SuccessUpdate(checksum.Checksum(sysent))).ToList());
  }
  
}