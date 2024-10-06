using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public record CoreAndPendingCreateMap(ICoreEntity Core, CoreToExternalMap.PendingCreate Map) {
  public CoreAndCreatedMap Created(string targetid) => new(Core, Map.SuccessCreate(targetid));
}

public record CoreAndCreatedMap {
  internal ICoreEntity Core { get; }
  internal CoreToExternalMap.Created Map { get; }
  
  internal CoreAndCreatedMap(ICoreEntity core, CoreToExternalMap.Created map) {
    Core = core;
    Map = map;
  }
}

public record CoreAndPendingUpdateMap(ICoreEntity Core, CoreToExternalMap.PendingUpdate Map) {
  // todo: add StagedCoreAndExternal and then this method should be removed
  public CoreExternalMap SetExternalEntity(ISystemEntity system, string checksum) => new(Core, system, Map with { Checksum = checksum });
}

public record CoreExternalMap(ICoreEntity Core, ISystemEntity SystemEntity, CoreToExternalMap.PendingUpdate Map) {
  public CoreAndUpdatedMap Updated() => new(Core, Map.SuccessUpdate());
}

public record CoreAndUpdatedMap {
  internal ICoreEntity Core { get; }
  internal CoreToExternalMap.Updated Map { get; }
  
  internal CoreAndUpdatedMap(ICoreEntity core, CoreToExternalMap.Updated map) {
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
  Task<ISystemEntity> CovertCoreEntityToExternalEntity(WriteOperationConfig config, ICoreEntity Core, ICoreToExternalMap Map);
  Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreAndPendingCreateMap> created, List<CoreExternalMap> updated);
}
