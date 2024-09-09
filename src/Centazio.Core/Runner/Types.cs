using System.Text.Json.Serialization;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Runner;

public record FunctionConfig<T>(SystemName System, LifecycleStage Stage, ValidList<T> Operations) where T : OperationConfig;

public abstract record OperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint);

public record ReadOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> GetObjectsToStage) : OperationConfig(Object, Cron, FirstTimeCheckpoint);


public record PromoteOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<PromoteOperationConfig>, IEnumerable<StagedEntity>, Task<PromoteOperationResult>> PromoteObjects) : OperationConfig(Object, Cron, FirstTimeCheckpoint);

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

public interface IOperationResult {
  public EOperationResult Result { get; } 
  public string Message { get; }
  public EResultType ResultType { get; }
  public int ResultLength { get; }
  public EOperationAbortVote AbortVote { get; }
  [property: JsonIgnore] public Exception? Exception { get; }
}

public abstract record OperationResult(
    EOperationResult Result, 
    string Message,
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    [property: JsonIgnore]
    Exception? Exception = null) : IOperationResult {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
}

public record ErrorReadOperationResult(string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : ReadOperationResult(EOperationResult.Error, Message, EResultType.Error, 0, AbortVote, Exception);

public abstract record ReadOperationResult(
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, ResultType, ResultLength, AbortVote, Exception) {
  
  public bool IsValid { get; } = Result != EOperationResult.Unknown && Result != EOperationResult.Error && ResultLength > 0; 
}

public record PromoteOperationResult(
    IEnumerable<StagedEntity> Promoted, 
    IEnumerable<(StagedEntity Entity, ValidString Reason)> Ignored,
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, ResultType, ResultLength, AbortVote, Exception);

public record EmptyReadOperationResult(string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : ReadOperationResult(EOperationResult.Success, Message, EResultType.Empty, 0, AbortVote, Exception);
public record SingleRecordReadOperationResult(ValidString Payload, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : ReadOperationResult(EOperationResult.Success, Message, EResultType.Single, Payload.Value.Length, AbortVote, Exception);
public record ListRecordsReadOperationResult(IReadOnlyList<string> PayloadList, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(
        EOperationResult.Success, 
        Message, 
        EResultType.List, 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote, 
        Exception) {
  public IReadOnlyList<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}



