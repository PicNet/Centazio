using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public record FunctionConfig<T>(SystemName System, LifecycleStage Stage, ValidList<T> Operations) where T : OperationConfig;

public abstract record OperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint);

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim(), CronFormat.IncludeSeconds); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig<T>(ObjectState State, T Settings) where T : OperationConfig {
  public DateTime Checkpoint => State.LastSuccessStart ?? Settings.FirstTimeCheckpoint;
}

public abstract record OperationResult(
    EOperationResult Result, 
    string Message,
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}