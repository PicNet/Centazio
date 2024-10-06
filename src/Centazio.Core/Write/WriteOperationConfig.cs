using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;


// todo: move containers to Containers.ts

public record CoreAndPendingCreateMap(ICoreEntity Core, CoreToSystemMap.PendingCreate Map) {
  public CoreAndCreatedMap Created(string targetid, SystemEntityChecksum checksum) => new(Core, Map.SuccessCreate(targetid, checksum));
}

public record CoreAndCreatedMap {
  public ICoreEntity Core { get; }
  public CoreToSystemMap.Created Map { get; }
  
  internal CoreAndCreatedMap(ICoreEntity core, CoreToSystemMap.Created map) {
    Core = core;
    Map = map;
  }
}

public record CoreAndPendingUpdateMap(ICoreEntity Core, CoreToSystemMap.PendingUpdate Map) {
  public CoreSystemMap SetSystemEntity(ISystemEntity system, SystemEntityChecksum checksum) => new(Core, system, Map with { Checksum = checksum });
}

public record CoreSystemMap(ICoreEntity Core, ISystemEntity SystemEntity, CoreToSystemMap.PendingUpdate Map) {
  public CoreAndUpdatedMap Updated(SystemEntityChecksum checksum) => new(Core, Map.SuccessUpdate(checksum));
}

public record CoreAndUpdatedMap {
  public ICoreEntity Core { get; }
  public CoreToSystemMap.Updated Map { get; }
  
  internal CoreAndUpdatedMap(ICoreEntity core, CoreToSystemMap.Updated map) {
    Core = core;
    Map = map;
  }
}

public record WriteOperationConfig(
    CoreEntityType CoreEntityType, 
    ValidCron Cron,
    ITargetSystemWriter TargetSysWriter) : OperationConfig(CoreEntityType, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public CoreEntityType CoreEntityType { get; init; } = CoreEntityType;
  
  public object LoggableValue => $"{CoreEntityType.Value}";

}

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface ITargetSystemWriter {
  Task<ISystemEntity> CovertCoreEntityToSystemEntity(WriteOperationConfig config, ICoreEntity Core, ICoreToSystemMap Map);
  Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreAndPendingCreateMap> created, List<CoreSystemMap> updated);
}
