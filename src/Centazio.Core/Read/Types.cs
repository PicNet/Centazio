using Centazio.Core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Func;

public abstract record BaseFunctionConfig(SystemName System, LifecycleStage Stage) {
}

public abstract record BaseFunctionOperationResult(string Message, Exception? Exception = null);

public record ReadFunctionConfig(SystemName System, LifecycleStage Stage, ValidList<ReadOperationConfig> Operations) : BaseFunctionConfig(System, Stage) {
}

public record ReadOperationConfig(ObjectName Object, ValidCron Cron, Func<DateTime, ReadOperationStateAndConfig, Task<ReadOperationResult>> Impl);

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(expression.Trim()); 
  }
  
  public CronExpression Value {get; }

}

public record ReadOperationStateAndConfig(ObjectState State, ReadOperationConfig Settings);

public abstract record ReadOperationResult(EOperationReadResult Result, string Message, EPayloadType PayloadType, int PayloadLength, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : BaseFunctionOperationResult(Message, Exception) {
  
  public EOperationReadResult Result { get; } = Result == EOperationReadResult.Unknown ? throw new ArgumentException("Result cannot be unknown") : Result;
  
  public ObjectState UpdateObjectState(ObjectState state, DateTime start) {
    return state with {
      LastStart = start,
      LastCompleted = UtcDate.UtcNow,
      LastResult = Result,
      LastAbortVote = AbortVote,
      LastRunMessage = $"read operation [{state.System}/{state.Stage}/{state.Object}] completed [{Result}] message: " + Message,
      LastPayLoadType = PayloadType,
      LastPayLoadLength = PayloadLength,
      LastRunException = Exception?.ToString()
    };
  }
}

public record EmptyReadOperationResult(EOperationReadResult Result, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.Empty, 0, AbortVote, Exception);

public record SingleRecordReadOperationResult(EOperationReadResult Result, string Message, ValidString Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.Single, Payload.Value.Length, AbortVote, Exception);

public record ListRecordReadOperationResult(EOperationReadResult Result, string Message, ValidList<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.List, PayloadList.Value.Count, AbortVote, Exception);
