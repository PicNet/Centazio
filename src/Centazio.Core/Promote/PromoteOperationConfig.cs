using Centazio.Core.CoreRepo;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public record EntityForPromotionEvaluation(ISystemEntity SystemEntity, ICoreEntity? ExistingCoreEntity) {
  public EntityEvaluationResult MarkForPromotion(ICoreEntity updated) => new EntityToPromote(SystemEntity, updated);
  public EntityEvaluationResult MarkForIgnore(ValidString reason) => new EntityToIgnore(SystemEntity, reason);
}

public abstract record EntityEvaluationResult(ISystemEntity SystemEntity);
public sealed record EntityToPromote(ISystemEntity SystemEntity, ICoreEntity UpdatedCoreEntity) : EntityEvaluationResult(SystemEntity);
public sealed record EntityToIgnore(ISystemEntity SystemEntity, ValidString IgnoreReason) : EntityEvaluationResult(SystemEntity);


public interface IEvaluateEntitiesToPromote {
  Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval);
}

public record PromoteOperationConfig(
    Type SystemEntityType, 
    SystemEntityTypeName SystemEntityTypeName,
    CoreEntityTypeName CoreEntityTypeName,
    ValidCron Cron, 
    IEvaluateEntitiesToPromote PromoteEvaluator) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {

  public SystemEntityTypeName SystemEntityTypeName { get; } = SystemEntityTypeName;
  public CoreEntityTypeName CoreEntityTypeName { get; } = CoreEntityTypeName;
  
  /// <summary>
  /// Biderectional objects do not check for bounce-backs.  This means that there is a risk
  /// that a change in system 1 will propegate to system 2 and then back to system 1 (and so on).
  ///
  /// Care must be taken that the core entities checksum takes care of this and that the change
  /// from system 1 and its bounce back produce the same checksum.
  /// </summary>
  public bool IsBidirectional { get; init; }
  
  public string LoggableValue => $"{SystemEntityTypeName.Value} -> {CoreEntityTypeName.Value}";

}

