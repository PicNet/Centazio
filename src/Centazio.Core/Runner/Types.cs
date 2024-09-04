using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Cronos;

namespace centazio.core.Runner;

public record FunctionConfig(SystemName System, LifecycleStage Stage, ValidList<OperationConfig> Operations);

public record OperationConfig(ObjectName Object, ValidCron Cron, Func<DateTime, OperationStateAndConfig, Task<OperationResult>> Impl);

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim()); 
  }
  
  public CronExpression Value {get; }

  public static implicit operator CronExpression(ValidCron value) => value.Value;
  public static implicit operator ValidCron(string value) => new(value);
}

public record OperationStateAndConfig(ObjectState State, OperationConfig Settings);

public abstract record OperationResult(EOperationResult Result, string Message, EPayloadType PayloadType, int PayloadLength, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) {
  
  public EOperationResult Result { get; } = Result == EOperationResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
  
  public ObjectState UpdateObjectState(ObjectState state, DateTime start) {
    return state with {
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = Result,
      LastAbortVote = AbortVote,
      LastRunMessage = $"operation [{state.System}/{state.Stage}/{state.Object}] completed [{Result}] message: " + Message,
      LastPayLoadType = PayloadType,
      LastPayLoadLength = PayloadLength,
      LastRunException = Exception?.ToString()
    };
  }
}

public record EmptyOperationResult(EOperationResult Result, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EPayloadType.Empty, 0, AbortVote, Exception);

public record SingleRecordOperationResult(EOperationResult Result, string Message, ValidString Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EPayloadType.Single, Payload.Value.Length, AbortVote, Exception);

public record ListRecordOperationResult(EOperationResult Result, string Message, ValidList<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : OperationResult(Result, Message, EPayloadType.List, PayloadList.Value.Count, AbortVote, Exception);
