using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public interface IGetObjectsToStage {
  Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config);
}

public record ReadOperationConfig(SystemEntityType SystemEntityType, ValidCron Cron, IGetObjectsToStage GetObjectsToStage) 
        : OperationConfig(SystemEntityType, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public SystemEntityType SystemEntityType { get; init; } = SystemEntityType;
  
  public string LoggableValue => $"{SystemEntityType.Value}";

}
        