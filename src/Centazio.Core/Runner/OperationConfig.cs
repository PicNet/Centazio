namespace Centazio.Core.Runner;

public record OpChangeTriggerKey(ObjectName Object, LifecycleStage Stage);

public abstract record OperationConfig(ObjectName Object, List<OpChangeTriggerKey> Triggers, ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
  public List<OpChangeTriggerKey> Triggers { get; set; } = Triggers;
}

public record OperationStateAndConfig<C>(ObjectState State, FunctionConfig FuncConfig, C OpConfig, DateTime Checkpoint) where C : OperationConfig;