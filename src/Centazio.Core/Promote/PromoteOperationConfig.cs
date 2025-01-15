using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Types;

namespace Centazio.Core.Promote;

public record EntityForPromotionEvaluation(ISystemEntity SystemEntity, CoreEntityAndMeta? ExistingCoreEntityAndMeta) {
  public EntityEvaluationResult MarkForPromotion(EntityForPromotionEvaluation eval, SystemName system, ICoreEntity core, Func<ICoreEntity, CoreEntityChecksum> checksum) => 
      new EntityToPromote(
          SystemEntity, 
          eval.ExistingCoreEntityAndMeta?.Update(system, core, checksum) 
              ?? CoreEntityAndMeta.Create(system, eval.SystemEntity.SystemId, core, checksum));
  
  public EntityEvaluationResult MarkForIgnore(ValidString reason) => new EntityToIgnore(SystemEntity, reason);
}

public abstract record EntityEvaluationResult(ISystemEntity SystemEntity);
public sealed record EntityToPromote(ISystemEntity SystemEntity, CoreEntityAndMeta CoreEntityAndMeta) : EntityEvaluationResult(SystemEntity);
public sealed record EntityToIgnore(ISystemEntity SystemEntity, ValidString IgnoreReason) : EntityEvaluationResult(SystemEntity);


public delegate Task<List<EntityEvaluationResult>> BuildCoreEntitiesHandler(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval);

public record PromoteOperationConfig(
    Type SystemEntityType, 
    SystemEntityTypeName SystemEntityTypeName,
    CoreEntityTypeName CoreEntityTypeName,
    ValidCron Cron, 
    BuildCoreEntitiesHandler BuildCoreEntities) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {

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

