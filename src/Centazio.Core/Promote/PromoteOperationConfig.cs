using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote<E> where E : ICoreEntity {
  Task<PromoteOperationResult<E>> Evaluate(OperationStateAndConfig<PromoteOperationConfig<E>> config, IEnumerable<StagedEntity> staged);
}

public record PromoteOperationConfig<E>(
    ObjectName Object, 
    ValidCron Cron, 
    IEvaluateEntitiesToPromote<E> EvaluateEntitiesToPromote) : OperationConfig(Object, Cron) where E : ICoreEntity;

