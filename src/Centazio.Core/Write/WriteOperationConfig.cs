using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint) : OperationConfig(Object, Cron, FirstTimeCheckpoint);

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface IWriteSingleEntityToTargetSystem<E> where E : ICoreEntity {
  Task<WriteOperationResult<E>> WriteEntities(
          SingleWriteOperationConfig<E> config, 
          List<(E Core, EntityIntraSysMap.PendingCreate Map)> created,
          List<(E Core, EntityIntraSysMap.PendingUpdate Map)> updated);
}

public record SingleWriteOperationConfig<E>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    IWriteSingleEntityToTargetSystem<E> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron, FirstTimeCheckpoint) where E : ICoreEntity;

// BatchWriteOperationConfig/IWriteBatchEntitiesToTargetSystem - used when target system handles batches of entities

public interface IWriteBatchEntitiesToTargetSystem<E> where E : ICoreEntity {
    Task<WriteOperationResult<E>> WriteEntities(
            BatchWriteOperationConfig<E> config, 
            List<(E Core, EntityIntraSysMap.PendingCreate Map)> created,
            List<(E Core, EntityIntraSysMap.PendingUpdate Map)> updated);
}

public record BatchWriteOperationConfig<E>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    IWriteBatchEntitiesToTargetSystem<E> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron, FirstTimeCheckpoint) where E : ICoreEntity;