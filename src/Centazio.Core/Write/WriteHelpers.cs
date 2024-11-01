using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Write;

public static class WriteHelpers {

  public static CovertCoreEntitiesToSystemEntitiesResult CovertCoreEntitiesToSystemEntitties<E>(
      List<CoreAndPendingCreateMap> tocreate, 
      List<CoreAndPendingUpdateMap> toupdate,
      IChecksumAlgorithm checksum,
      // todo: this string should be SystemEntityId and FromCore should be called something better, like `ConvertCoreToSystemEntityForWriting`
      Func<string, E, ISystemEntity> FromCore) where E : ICoreEntity {
    return new(
      tocreate.Select(m => {
        var core = m.CoreEntity.To<E>();
        var sysent = FromCore(String.Empty, core);
        return m.AddSystemEntity(sysent);
      }).ToList(),
      toupdate.Select(m => {
        var core = m.CoreEntity.To<E>();
        var sysent = FromCore(m.Map.SystemId, core);
        if (m.Map.SystemEntityChecksum == checksum.Checksum(sysent)) throw new Exception($"No changes found on [{typeof(E).Name}] -> [{sysent.GetType().Name}]:" + 
          $"\n\tUpdated Core Entity:[{Json.Serialize(core)}]" +
          $"\n\tUpdated Sys Entity[{sysent}]" +
          $"\n\tExisting Checksum:[{m.Map.SystemEntityChecksum}]" +
          $"\n\tChecksum Subset[{sysent.GetChecksumSubset()}]" +
          $"\n\tChecksum[{checksum.Checksum(sysent)}]");
        
        return m.AddSystemEntity(sysent);
      }).ToList());
  }
  
  public static SuccessWriteOperationResult GetSuccessWriteOperationResult(
      List<CoreSystemAndPendingCreateMap> tocreate, IEnumerable<ISystemEntity> created, 
      List<CoreSystemAndPendingUpdateMap> toupdate, IEnumerable<ISystemEntity> updated,
      IChecksumAlgorithm chksm) {
    // todo-low: this is a bit dodgy, its expecting the created/updated entities to come back in the same order as pre change entities
    return new SuccessWriteOperationResult(
          created.Select((sysent, idx) => tocreate[idx].Map.SuccessCreate(sysent.SystemId, chksm.Checksum(sysent))).ToList(), 
          updated.Select((sysent, idx) => toupdate[idx].Map.SuccessUpdate(chksm.Checksum(sysent))).ToList());
  }
}