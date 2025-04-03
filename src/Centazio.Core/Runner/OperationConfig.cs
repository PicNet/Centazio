namespace Centazio.Core.Runner;

public abstract record FunctionTrigger;

public record ObjectChangeTrigger(ObjectName Object, LifecycleStage Stage) : FunctionTrigger {
  public override string ToString() => $"{Object.Value}/{Stage.Value}";
}

public record TimerChangeTrigger(string Expression) : FunctionTrigger {
  public override string ToString() => $"Timer Trigger [{Expression}]";
}

public abstract record OperationConfig(ObjectName Object, List<ObjectChangeTrigger> Triggers, ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
  public List<ObjectChangeTrigger> Triggers { get; set; } = Triggers;
}

public record OperationStateAndConfig<C>(ObjectState State, FunctionConfig FuncConfig, C OpConfig, DateTime Checkpoint) where C : OperationConfig;