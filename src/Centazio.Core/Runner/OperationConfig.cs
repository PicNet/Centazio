namespace Centazio.Core.Runner;

public abstract record FunctionTrigger;

public record ObjectChangeTrigger(SystemName System, LifecycleStage Stage, ObjectName Object) : FunctionTrigger {
  public override string ToString() => $"{System.Value}/{Object.Value}/{Stage.Value}";
  
  public bool Matches(ObjectChangeTrigger other) => 
      other.Stage == Stage && other.Object == Object 
          && (System is NotSystem ? other.System != System : other.System == System);
}

public record TimerChangeTrigger(string Expression) : FunctionTrigger {
  public override string ToString() => $"Timer Trigger [{Expression}]";
}

public abstract record OperationConfig(ObjectName Object, List<ObjectChangeTrigger> Triggers, ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
  public List<ObjectChangeTrigger> Triggers { get; set; } = Triggers;
  
  public abstract bool ShouldRunBasedOnTriggers(List<ObjectChangeTrigger> triggeredby);
}

public record OperationStateAndConfig<C>(ObjectState State, FunctionConfig FuncConfig, C OpConfig, DateTime Checkpoint) where C : OperationConfig;