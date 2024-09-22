using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron) : OperationConfig(Object, Cron);

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
    IWriteSingleEntityToTargetSystem<E> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron) where E : ICoreEntity;

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
    IWriteBatchEntitiesToTargetSystem<E> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron) where E : ICoreEntity;