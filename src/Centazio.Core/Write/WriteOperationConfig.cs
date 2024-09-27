using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public record CoreAndPendingCreateMap(ICoreEntity Core, EntityIntraSysMap.PendingCreate Map) {
  public CoreAndCreatedMap Created(string targetid) => new(Core, Map.SuccessCreate(targetid));
}
public record CoreAndCreatedMap {
  internal ICoreEntity Core { get; }
  internal EntityIntraSysMap.Created Map { get; }
  
  internal CoreAndCreatedMap(ICoreEntity core, EntityIntraSysMap.Created map) {
    Core = core;
    Map = map;
  }
}
public record CoreAndPendingUpdateMap(ICoreEntity Core, EntityIntraSysMap.PendingUpdate Map) {
  public CoreAndUpdatedMap Updated() => new(Core, Map.SuccessUpdate());
}

public record CoreAndUpdatedMap {
  internal ICoreEntity Core { get; }
  internal EntityIntraSysMap.Updated Map { get; }
  
  internal CoreAndUpdatedMap(ICoreEntity core, EntityIntraSysMap.Updated map) {
    Core = core;
    Map = map;
  }
}

public record WriteOperationConfig(
    CoreEntityType CoreEntityType, 
    ValidCron Cron,
    IWriteEntitiesToTargetSystem WriteEntitiesesToTargetSystem) : OperationConfig<CoreEntityType>(CoreEntityType, Cron) {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public CoreEntityType CoreEntityType { get; init; } = CoreEntityType;
}

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface IWriteEntitiesToTargetSystem {
  Task<WriteOperationResult> WriteEntities(
          WriteOperationConfig config, 
          List<CoreAndPendingCreateMap> created,
          List<CoreAndPendingUpdateMap> updated);
}
