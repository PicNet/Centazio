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

public abstract record WriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron) : OperationConfig(Object, Cron);

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface IWriteSingleEntityToTargetSystem {
  Task<WriteOperationResult> WriteEntities(
          SingleWriteOperationConfig config, 
          List<CoreAndPendingCreateMap> created,
          List<CoreAndPendingUpdateMap> updated);
}

public record SingleWriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    IWriteSingleEntityToTargetSystem WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron);

// BatchWriteOperationConfig/IWriteBatchEntitiesToTargetSystem - used when target system handles batches of entities

public interface IWriteBatchEntitiesToTargetSystem {
    Task<WriteOperationResult> WriteEntities(
            BatchWriteOperationConfig config, 
            List<CoreAndPendingCreateMap> created,
            List<CoreAndPendingUpdateMap> updated);
}

public record BatchWriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    IWriteBatchEntitiesToTargetSystem WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron);