using Centazio.Core.Checksum;
using Centazio.Core.Types;

namespace Centazio.Core.Ctl.Entities;

public record CoreAndPendingCreateMap(ICoreEntity CoreEntity, Map.PendingCreate Map) {
  public CoreSystemAndPendingCreateMap AddSystemEntity(ISystemEntity sysent, IChecksumAlgorithm checksum) => new(CoreEntity, sysent, Map, checksum);
}

public record CoreAndPendingUpdateMap(ICoreEntity CoreEntity,  Map.PendingUpdate Map) {
  public CoreSystemAndPendingUpdateMap AddSystemEntity(ISystemEntity system, IChecksumAlgorithm checksum) => new(CoreEntity, system, Map, checksum);
}

public record CoreSystemAndPendingCreateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity, Map.PendingCreate Map, IChecksumAlgorithm checksum) {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Created SuccessCreate(SystemEntityId newid) => Map.SuccessCreate(newid, checksum.Checksum(SystemEntity.CreatedWithId(newid)));

}

public record CoreSystemAndPendingUpdateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingUpdate Map, IChecksumAlgorithm checksum) {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Updated SuccessUpdate() => Map.SuccessUpdate(checksum.Checksum(SystemEntity)); 
}