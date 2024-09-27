using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote {
  Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, IEnumerable<StagedEntity> staged);
}

public record PromoteOperationConfig(
    ExternalEntityType ExternalEntityType,
    CoreEntityType CoreEntityType, 
    ValidCron Cron, 
    IEvaluateEntitiesToPromote EvaluateEntitiesToPromote) : OperationConfig<CoreEntityType>(CoreEntityType, Cron) {

  // ReSharper disable RedundantExplicitPositionalPropertyDeclaration
  public ExternalEntityType ExternalEntityType { get; init; } = ExternalEntityType;
  public CoreEntityType CoreEntityType { get; init; } = CoreEntityType;

}

