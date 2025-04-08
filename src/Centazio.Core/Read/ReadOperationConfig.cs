namespace Centazio.Core.Read;

public delegate Task<ReadOperationResult> GetUpdatesAfterCheckpointHandler(OperationStateAndConfig<ReadOperationConfig> config);

public record ReadOperationConfig(SystemEntityTypeName SystemEntityTypeName, ValidCron Cron, GetUpdatesAfterCheckpointHandler GetUpdatesAfterCheckpoint) 
        : OperationConfig(SystemEntityTypeName, [], Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public SystemEntityTypeName SystemEntityTypeName { get; init; } = SystemEntityTypeName;
  
  public string LoggableValue => $"{SystemEntityTypeName.Value}";
  
  // read function by default ignore triggers, as they are usually
  //    the first function in a chain that is triggered by a timer
  public override bool ShouldRunBasedOnTriggers(List<ObjectChangeTrigger> triggeredby) => true;

}
        