using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote {
  Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged);
}

public record PromoteOperationConfig(
    ExternalEntityType ExternalEntityType, 
    ValidCron Cron, 
    IEvaluateEntitiesToPromote EvaluateEntitiesToPromote) : OperationConfig(ExternalEntityType, Cron) {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public ExternalEntityType ExternalEntityType { get; init; } = ExternalEntityType;
}

