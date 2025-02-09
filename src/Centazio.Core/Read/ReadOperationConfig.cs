using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public delegate Task<ReadOperationResult> GetUpdatesAfterCheckpointHandler(OperationStateAndConfig<ReadOperationConfig> config);

public record ReadOperationConfig(SystemEntityTypeName SystemEntityTypeName, ValidCron Cron, GetUpdatesAfterCheckpointHandler GetUpdatesAfterCheckpoint) 
        : OperationConfig(SystemEntityTypeName, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public SystemEntityTypeName SystemEntityTypeName { get; init; } = SystemEntityTypeName;
  
  public string LoggableValue => $"{SystemEntityTypeName.Value}";

}
        