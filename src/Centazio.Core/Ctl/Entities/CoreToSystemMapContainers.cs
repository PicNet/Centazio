using Centazio.Core.CoreRepo;
using Centazio.Core.Types;

namespace Centazio.Core.Ctl.Entities;

public record CoreAndPendingCreateMap(ICoreEntity CoreEntity, Map.PendingCreate Map) {
  public CoreSystemAndPendingCreateMap AddSystemEntity(ISystemEntity sysent) => new(CoreEntity, sysent, Map);
}

public record CoreAndPendingUpdateMap(ICoreEntity CoreEntity,  Map.PendingUpdate Map) {
  public CoreSystemAndPendingUpdateMap AddSystemEntity(ISystemEntity system) => new(CoreEntity, system, Map);
}

public record CoreSystemAndPendingCreateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingCreate Map);
public record CoreSystemAndPendingUpdateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingUpdate Map);