using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote {
  Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged);
}

public record PromoteOperationConfig(
    ObjectName Object, 
    ValidCron Cron, 
    IEvaluateEntitiesToPromote EvaluateEntitiesToPromote) : OperationConfig(Object, Cron);

