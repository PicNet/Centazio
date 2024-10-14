using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public interface IGetObjectsToStage {
  Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config);
}

public record ReadOperationConfig(SystemEntityTypeName SystemEntityTypeName, ValidCron Cron, IGetObjectsToStage GetObjectsToStage) 
        : OperationConfig(SystemEntityTypeName, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public SystemEntityTypeName SystemEntityTypeName { get; init; } = SystemEntityTypeName;
  
  public string LoggableValue => $"{SystemEntityTypeName.Value}";

}
        