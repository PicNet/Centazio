namespace Centazio.Core.Runner;

public abstract record OperationConfig(ObjectName Object, List<(ObjectName, LifecycleStage)> Triggers, ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
}

public record OperationStateAndConfig<C>(ObjectState State, FunctionConfig FuncConfig, C OpConfig, DateTime Checkpoint) where C : OperationConfig;