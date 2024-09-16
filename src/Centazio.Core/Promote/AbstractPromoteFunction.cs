using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract class AbstractPromoteFunction<C>(IOperationsFilterAndPrioritiser<PromoteOperationConfig<C>>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig<C>, PromoteOperationResult<C>>(prioritiser) where C : ICoreEntity;

public record PromoteOperationConfig<C>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint, 
    Func<OperationStateAndConfig<PromoteOperationConfig<C>>, IEnumerable<StagedEntity>, Task<PromoteOperationResult<C>>> EvaluateEntitiesToPromote) : OperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;

