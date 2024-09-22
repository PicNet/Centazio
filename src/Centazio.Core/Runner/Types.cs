using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public record FunctionConfig<T>(
    SystemName System, 
    LifecycleStage Stage, 
    ValidList<T> Operations) where T : OperationConfig {

  /// <summary>
  /// This is the maximum number of minutes that a function can be considered `running`.  After this, we assume
  /// the function is somehow stuck, and allow other instances of this function to run again.
  /// </summary>
  public int TimeoutMinutes { get; init; } = 15;

  /// <summary>
  /// When a function operation is run for the first time, there is no 'last run'.  This is used as a
  /// replacement for this 'last run' value and any data since this date is considered ready for processing.
  /// Note: This can be overwritten in each operation's specific `OperationConfig`.
  /// </summary>
  public DateTime DefaultFirstTimeCheckpoint { get; init; } = UtcDate.UtcNow.AddMonths(-1); 

}

public abstract record OperationConfig(
    ObjectName Object, 
    ValidCron Cron) {
  public DateTime? FirstTimeCheckpoint { get; init; }
}

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim(), CronFormat.IncludeSeconds); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig<T>(ObjectState State, T Settings, DateTime Checkpoint) where T : OperationConfig {
  
  // This constructor is mainly used for unit testing where Checkpoint is not usually a factor (hence internal) 
  internal OperationStateAndConfig(ObjectState State, T Settings) : this(State, Settings, DateTime.MinValue) {}
}

public abstract record OperationResult(
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}