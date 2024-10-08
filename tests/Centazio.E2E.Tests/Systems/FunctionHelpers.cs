using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Misc;

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
        var sysent = FromCore(String.Empty, m.Core.To<E>()); 
        return m.AddSystemEntity(sysent);
      }).ToList(),
      toupdate.Select(m => {
        var sysent = FromCore(m.Map.SystemId, m.Core.To<E>());
        TestEntityHasChanges(sysent, m.Map.SystemEntityChecksum);
        return m.AddSystemEntity(sysent);
      }).ToList());
  }
  
  /// <summary>
  /// This method compares the checksum of the entity to be updated with the checksum in the database (Mapping table).
  /// We originally tried to compare the checksum with the state of the entity in the target system, however this is not
  /// valid as the same change can be made in both the source and target system causing this check to fail. 
  /// </summary>
  private void TestEntityHasChanges<E>(E updated, SystemEntityChecksum? existingcs) where E : ISystemEntity {
    if (existingcs is null) throw new ArgumentNullException($"TestEntityHasChanges[{typeof(E).Name}] has null 'Existing Checksum (existingcs)'.  When editing entities this parameter is mandatory.");
    
    if (existingcs == checksum.Checksum(updated)) 
      throw new Exception($"TestEntityHasChanges[{system}/{updated.GetType().Name}] updated object with no changes." +
        $"\nExisting Checksum (In Db):\n\t{existingcs}" +
        $"\nUpdated:\n\t{updated}({updated.GetChecksumSubset()}#{checksum.Checksum(updated)})");
  }
  
  public async Task<Dictionary<ValidString, ValidString>> GetRelatedEntitySystemIdsFromCoreIds(List<ICoreEntity> entities, string foreignkey, CoreEntityType obj) {
    var fks = entities.Select(e => ReflectionUtils.GetPropValAsString(e, foreignkey)).Distinct().ToList();
    var maps = await intra.GetExistingMappingsFromCoreIds(obj, fks, system);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SystemId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {obj} with ids [{String.Join(",", missing)}]");
    
    return dict;
  } 
 
  public async Task<Dictionary<ValidString, ValidString>> GetRelatedEntityCoreIdsFromSystemIds<E>(List<E> entities, string foreignkey, CoreEntityType obj) where E : ISystemEntity {
    var fks = entities.Select(e => ReflectionUtils.GetPropValAsString(e, foreignkey)).Distinct().ToList();
    // we do not check for missing ids here as this method is called during promotion, and it is possible for these entities not to be in core storage as they are being created
    return (await intra.GetExistingMappingsFromCoreIds(obj, fks, system)).ToDictionary(m => m.SystemId, m => m.CoreId);
  } 
  
}