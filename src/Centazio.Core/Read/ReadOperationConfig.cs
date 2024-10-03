using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public interface IGetObjectsToStage {
  Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config);
}

public record ReadOperationConfig(ExternalEntityType ExternalEntityType, ValidCron Cron, IGetObjectsToStage GetObjectsToStage) 
        : OperationConfig(ExternalEntityType, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public ExternalEntityType ExternalEntityType { get; init; } = ExternalEntityType;
  
  public object LoggableValue => $"{ExternalEntityType.Value}";

}
        