using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Types;

namespace Centazio.Core.Promote;

public record EntityForPromotionEvaluation {

  public ISystemEntity SystemEntity { get; }
  public CoreEntityAndMeta? ExistingCoreEntityAndMeta { get; }
  
  private IChecksumAlgorithm ChecksumAlgo { get; }
  
  public EntityForPromotionEvaluation(ISystemEntity sysent, CoreEntityAndMeta? coreent, IChecksumAlgorithm checksum) {
    SystemEntity = sysent;
    ExistingCoreEntityAndMeta = coreent;
    ChecksumAlgo = checksum;
  }
  
  public EntityEvaluationResult MarkForPromotion(SystemName system, ICoreEntity core) {
    return new EntityToPromote(SystemEntity,
        ExistingCoreEntityAndMeta?.Update(system, core, ChecksumAlgo.Checksum)
        ?? CoreEntityAndMeta.Create(system, SystemEntity.SystemId, core, ChecksumAlgo.Checksum));
  }

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
  
  
  public Type SystemEntityType { get; } = ValidateSystemEntityTypeImplementsISystemEntity(SystemEntityType);
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

  private static Type ValidateSystemEntityTypeImplementsISystemEntity(Type type) => 
      type.GetInterfaces().FirstOrDefault(i => i == typeof(ISystemEntity)) is not null ? type : throw new Exception($"PromoteOperationConfig.SystemEntityType must be a `Type` that implements the ISystemEntity interface");

}

