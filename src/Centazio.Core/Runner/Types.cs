using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public record FunctionConfigDefaults {
  public static bool ThrowExceptions { get; set; }
  public static int TimeoutMinutes { get; set; } = 15;
  public static DateTime DefaultFirstTimeCheckpoint { get; set; } = UtcDate.UtcNow.AddMonths(-1);
}

public record FunctionConfig<C, O>(
    SystemName System, 
    LifecycleStage Stage, 
    List<C> Operations) : ILoggable 
        where C : OperationConfig<O>
        where O : ObjectName {

  public List<C> Operations { get; } = Operations.Any() ? Operations : throw new ArgumentNullException(nameof(Operations));
  
  /// <summary>
  /// This is the maximum number of minutes that a function can be considered `running`.  After this, we assume
  /// the function is somehow stuck, and allow other instances of this function to run again.
  /// </summary>
  public int TimeoutMinutes { get; init; } = FunctionConfigDefaults.TimeoutMinutes;

  /// <summary>
  /// When a function operation is run for the first time, there is no 'last run'.  This is used as a
  /// replacement for this 'last run' value and any data since this date is considered ready for processing.
  /// Note: This can be overwritten in each operation's specific `OperationConfig`.
  /// </summary>
  public DateTime DefaultFirstTimeCheckpoint { get; init; } = FunctionConfigDefaults.DefaultFirstTimeCheckpoint;

  /// <summary>
  /// Whether to log and swallow exceptions, or throw instead.  Logging exceptions works well
  /// for serverless environments where exceptions add no value, but on local hosted or testing
  /// environments Exceptions add more value.
  /// </summary>
  public bool ThrowExceptions { get; init; } = FunctionConfigDefaults.ThrowExceptions;

  public object LoggableValue => $"{System}/{Stage} Operations[{Operations.Count}]";
}

public abstract record OperationConfig<O>(
    O Object, 
    ValidCron Cron) where O : ObjectName {
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

public record OperationStateAndConfig<C, O>(ObjectState<O> State, C Config, DateTime Checkpoint) 
    where C : OperationConfig<O> 
    where O : ObjectName {
  
  // This constructor is mainly used for unit testing where Checkpoint is not usually a factor (hence internal) 
  internal OperationStateAndConfig(ObjectState<O> State, C Config) : this(State, Config, DateTime.MinValue) {}
}

public abstract record OperationResult(
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}