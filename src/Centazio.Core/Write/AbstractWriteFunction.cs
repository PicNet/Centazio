using Centazio.Core.CoreRepo;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract class AbstractWriteFunction<C>(IOperationsFilterAndPrioritiser<WriteOperationConfig<C>>? prioritiser = null) 
    : AbstractFunction<WriteOperationConfig<C>, WriteOperationResult<C>>(prioritiser) where C : ICoreEntity;

public abstract record WriteOperationConfig<C>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint) : OperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;


public record SingleWriteOperationConfig<C>(ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    Func<
        SingleWriteOperationConfig<C>, 
        List<C>, 
        IWriteSingleEntityToTargetSystemCallback<C>, 
        Task<WriteOperationResult<C>>> WriteEntitiesToTargetSystem) : WriteOperationConfig<C>(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity {}

public record BatchWriteOperationConfig<C>(ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    Func<
        BatchWriteOperationConfig<C>, 
        List<C>,
        IWriteBatchEntiiestToTargetSystemCallback<C>, 
        Task<WriteOperationResult<C>>> WriteEntitiesToTargetSystem) : WriteOperationConfig<C>(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity {}