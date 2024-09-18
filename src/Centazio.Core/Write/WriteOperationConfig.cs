using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint) : OperationConfig(Object, Cron, FirstTimeCheckpoint);


public record SingleWriteOperationConfig<C>(ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    Func<
        SingleWriteOperationConfig<C>, 
        List<(C Core, EntityIntraSystemMapping Map)>, 
        Task<WriteOperationResult<C>>> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;

public record BatchWriteOperationConfig<C>(ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    Func<
        BatchWriteOperationConfig<C>, 
        List<(C Core, EntityIntraSystemMapping Map)>,
        Task<WriteOperationResult<C>>> WriteEntitiesToTargetSystem) : WriteOperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;