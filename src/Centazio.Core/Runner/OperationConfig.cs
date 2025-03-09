namespace Centazio.Core.Runner;

public abstract record OperationConfig(
    ObjectName Object, 
    ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
}

public record OperationStateAndConfig<C>(
    ObjectState State,
    IFunctionConfig FuncConfig,
    C OpConfig, 
    DateTime Checkpoint) where C : OperationConfig;