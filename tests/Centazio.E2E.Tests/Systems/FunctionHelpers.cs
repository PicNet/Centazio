using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Misc;
using Centazio.Core.Write;

namespace Centazio.E2E.Tests.Systems;

public class FunctionHelpers(SimulationCtx ctx, SystemName system) {
  
  public (List<CoreSysAndPendingCreateMap>, List<CoreSystemMap>) CovertCoreEntitiesToSystemEntitties<E>(
      List<CoreAndPendingCreateMap> tocreate, 
      List<CoreAndPendingUpdateMap> toupdate,
      Func<string, E, ISystemEntity> FromCore) where E : ICoreEntity {
    return (
      tocreate.Select(m => {
        var sysent = FromCore(String.Empty, m.Core.To<E>()); 
        return m.AddSystemEntity(sysent, ctx.checksum.Checksum(sysent));
      }).ToList(),
      toupdate.Select(m => m.SetSystemEntity(FromCore(m.Map.SysId, m.Core.To<E>()))).ToList());
  }
  
  public E TestEntityHasChanges<E>(E updated, ISimulationSystem sys) where E : ISystemEntity {
    if (updated.SystemId == Guid.Empty.ToString() || updated.SystemId == "0") return updated;
    var existing = sys.GetEntities<E>().Single(c => updated.SystemId == c.SystemId);
    if (ctx.checksum.Checksum(existing) == ctx.checksum.Checksum(updated)) 
      throw new Exception($"TestEntityHasChanges[{system}/{updated.GetType().Name}] updated object with no changes." +
        $"\nExisting:\n\t{existing}({existing.GetChecksumSubset()})" +
        $"\nUpdated:\n\t{updated}({updated.GetChecksumSubset()})");
    return updated;
  }
  
  public async Task<Dictionary<ValidString, ValidString>> GetRelatedEntitySystemIdsFromCoreIds(ICoreToSystemMapStore intra, List<ICoreEntity> entities, string foreignkey, CoreEntityType obj) {
    var fks = entities.Select(e => ReflectionUtils.GetPropValAsString(e, foreignkey)).Distinct().ToList();
    var maps = await intra.GetExistingMappingsFromCoreIds(obj, fks, system);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SysId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {obj} with ids [{String.Join(",", missing)}]");
    
    return dict;
  } 
 
  public async Task<Dictionary<ValidString, ValidString>> GetRelatedEntityCoreIdsFromSystemIds<E>(ICoreToSystemMapStore intra, List<E> entities, string foreignkey, CoreEntityType obj) where E : ISystemEntity {
    var fks = entities.Select(e => ReflectionUtils.GetPropValAsString(e, foreignkey)).Distinct().ToList();
    // we do not check for missing ids here as this method is called during promotion, and it is possible for these entities not to be in core storage as they are being created
    return (await intra.GetExistingMappingsFromCoreIds(obj, fks, system)).ToDictionary(m => m.SysId, m => m.CoreId);
  } 
  
}