using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron) : OperationConfig(Object, Cron);

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface IWriteSingleEntityToTargetSystem {
  Task<WriteOperationResult> WriteEntities(
          SingleWriteOperationConfig config, 
          List<(ICoreEntity Core, EntityIntraSysMap.PendingCreate Map)> created,
          List<(ICoreEntity Core, EntityIntraSysMap.PendingUpdate Map)> updated);
}

public record SingleWriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    IWriteSingleEntityToTargetSystem WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron);

// BatchWriteOperationConfig/IWriteBatchEntitiesToTargetSystem - used when target system handles batches of entities

public interface IWriteBatchEntitiesToTargetSystem {
    Task<WriteOperationResult> WriteEntities(
            BatchWriteOperationConfig config, 
            List<(ICoreEntity Core, EntityIntraSysMap.PendingCreate Map)> created,
            List<(ICoreEntity Core, EntityIntraSysMap.PendingUpdate Map)> updated);
}

public record BatchWriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    IWriteBatchEntitiesToTargetSystem WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron);