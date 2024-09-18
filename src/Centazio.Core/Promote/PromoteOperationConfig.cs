using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public record PromoteOperationConfig<C>(
    ObjectName Object, 
    ValidCron Cron, 
    DateTime FirstTimeCheckpoint,
    // todo: these Func signatures are so ugly, think of a better approach (interface perhaps)
    Func<OperationStateAndConfig<PromoteOperationConfig<C>>, IEnumerable<StagedEntity>, Task<PromoteOperationResult<C>>> EvaluateEntitiesToPromote) : OperationConfig(Object, Cron, FirstTimeCheckpoint) where C : ICoreEntity;

