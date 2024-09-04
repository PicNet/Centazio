using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace centazio.core.Runner;

public record FunctionConfig<T>(SystemName System, LifecycleStage Stage, ValidList<T> Operations) where T : OperationConfig;

public record OperationConfig(ObjectName Object, ValidCron Cron);
public record ReadOperationConfig(ObjectName Object, ValidCron Cron, Func<DateTime, OperationStateAndConfig<ReadOperationConfig>, Task<OperationResult>> Impl) : OperationConfig(Object, Cron);
public record PromoteOperationConfig(ObjectName Object, ValidCron Cron, Func<DateTime, OperationStateAndConfig<PromoteOperationConfig>, IEnumerable<StagedEntity>, Task<OperationResult>> Impl) : OperationConfig(Object, Cron);

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim()); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig<T>(ObjectState State, T Settings) where T : OperationConfig;

public abstract record OperationResult(EOperationResult Result, string Message, EResultType ResultType, int ResultLength, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
  
  public ObjectState UpdateObjectState(ObjectState state, DateTime start) {
    return state with {
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = Result,
      LastAbortVote = AbortVote,
      LastRunMessage = $"operation [{state.System}/{state.Stage}/{state.Object}] completed [{Result}] message: {Message}",
      LastPayLoadType = ResultType,
      LastPayLoadLength = ResultLength,
      LastRunException = Exception?.ToString()
    };
  }
}

public record EmptyOperationResult(EOperationResult Result, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EResultType.Empty, 0, AbortVote, Exception);

public record SingleRecordOperationResult(EOperationResult Result, string Message, ValidString Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EResultType.Single, Payload.Value.Length, AbortVote, Exception);

public record ListRecordOperationResult(EOperationResult Result, string Message, ValidList<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EResultType.List, PayloadList.Value.Count, AbortVote, Exception);
