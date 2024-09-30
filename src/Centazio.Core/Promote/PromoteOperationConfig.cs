using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote {
  Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, List<StagedEntity> staged);
}

public record PromoteOperationConfig(
    ExternalEntityType ExternalEntityType,
    CoreEntityType CoreEntityType,
    ValidCron Cron, 
    IEvaluateEntitiesToPromote EvaluateEntitiesToPromote) : OperationConfig<CoreEntityType>(CoreEntityType, Cron), ILoggable {

  // ReSharper disable RedundantExplicitPositionalPropertyDeclaration
  public ExternalEntityType ExternalEntityType { get; } = ExternalEntityType;
  public CoreEntityType CoreEntityType { get; } = CoreEntityType;
  
  /// <summary>
  /// Biderectional objects do not check for bounce-backs.  This means that there is a risk
  /// that a change in system 1 will propegate to system 2 and then back to system 1 (and so on).
  ///
  /// Care must be taken that the core entities checksum takes care of this and that the change
  /// from system 1 and its bounce back produce the same checksum.
  /// </summary>
  public bool IsBidirectional { get; init; }
  
  public object LoggableValue => $"{ExternalEntityType.Value} -> {CoreEntityType.Value}";

}

