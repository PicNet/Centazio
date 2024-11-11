using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public record ReadOperationConfig(SystemEntityTypeName SystemEntityTypeName, ValidCron Cron, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> GetUpdatesAfterCheckpoint) 
        : OperationConfig(SystemEntityTypeName, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public SystemEntityTypeName SystemEntityTypeName { get; init; } = SystemEntityTypeName;
  
  public string LoggableValue => $"{SystemEntityTypeName.Value}";

}
        