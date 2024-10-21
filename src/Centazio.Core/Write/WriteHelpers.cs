using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Write;

public static class WriteHelpers {

  public static CovertCoreEntitiesToSystemEntitiesResult CovertCoreEntitiesToSystemEntitties<E>(
      List<CoreAndPendingCreateMap> tocreate, 
      List<CoreAndPendingUpdateMap> toupdate,
      IChecksumAlgorithm checksum,
      Func<string, E, ISystemEntity> FromCore) where E : ICoreEntity {
    return new(
      tocreate.Select(m => m.AddSystemEntity(FromCore(String.Empty, m.CoreEntity.To<E>()))).ToList(),
      toupdate.Select(m => {
        var sysent = FromCore(m.Map.SystemId, m.CoreEntity.To<E>());
        TestEntityHasChanges(sysent, m.Map.SystemEntityChecksum, checksum);
        return m.AddSystemEntity(sysent);
      }).ToList());
  }
  
  public static SuccessWriteOperationResult GetSuccessWriteOperationResult(
      List<CoreSystemAndPendingCreateMap> tocreate, IEnumerable<ISystemEntity> created, 
      List<CoreSystemAndPendingUpdateMap> toupdate, IEnumerable<ISystemEntity> updated,
      IChecksumAlgorithm chksm) {
    // todo: this is a bit dodgy, its expecting the created/updated entities to come back in the same order as pre change entities
    return new SuccessWriteOperationResult(
          created.Select((sysent, idx) => tocreate[idx].Map.SuccessCreate(sysent.SystemId, chksm.Checksum(sysent))).ToList(), 
          updated.Select((sysent, idx) => toupdate[idx].Map.SuccessUpdate(chksm.Checksum(sysent))).ToList());
  }
  
  /// <summary>
  /// This method compares the checksum of the entity to be updated with the checksum in the database (Mapping table).
  /// We originally tried to compare the checksum with the state of the entity in the target system, however this is not
  /// valid as the same change can be made in both the source and target system causing this check to fail. 
  /// </summary>
  private static void TestEntityHasChanges(ISystemEntity sysent, SystemEntityChecksum originalchksm, IChecksumAlgorithm checksum) {
    if (originalchksm != checksum.Checksum(sysent)) return;
    
    throw new Exception($"No changes found on {sysent.GetType().Name}:" +
      $"\n\tExisting Checksum:[{originalchksm}]" +
      $"\n\tUpdated[{sysent}]\n\tChecksum Subset[{sysent.GetChecksumSubset()}]" +
      $"\n\tChecksum[{checksum.Checksum(sysent)}]");
  }

}