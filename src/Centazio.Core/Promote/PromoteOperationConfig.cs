using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public interface IEvaluateEntitiesToPromote {
  Task<PromoteOperationResult> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<Containers.StagedSysOptionalCore> staged);
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

