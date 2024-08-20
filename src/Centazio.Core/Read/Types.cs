using centazio.core.Ctl.Entities;

namespace Centazio.Core.Func;

public record ReadFunctionConfig(SystemName System, List<ReadOperationConfig> Operations);

public record ReadOperationConfig(ObjectName Object, string Cron, int Limit);

public record ReadOperationStateAndConfig(ObjectState State, ReadOperationConfig Settings);

public abstract record ReadOperationResults(EOperationReadResult Result, string Message, EPayloadType PayloadType, int PayloadLength, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) {
  public void Validate() {
    if (PayloadType != EPayloadType.Empty && PayloadLength == 0) throw new Exception($"When an ReadOperationResult is not EPayloadType.Empty then the PayloadLength should be greater than 0");
    if (PayloadType == EPayloadType.Empty && PayloadLength != 0) throw new Exception($"When an ReadOperationResult is EPayloadType.Empty then the PayloadLength should be 0");
  }
}

public record EmptyReadOperationResults(EOperationReadResult Result, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResults(Result, Message, EPayloadType.Empty, 0, AbortVote, Exception);

public record SingleRecordReadOperationResults(EOperationReadResult Result, string Message, string Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResults(Result, Message, EPayloadType.Single, Payload.Length, AbortVote, Exception);

public record ListRecordReadOperationResults(EOperationReadResult Result, string Message, List<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResults(Result, Message, EPayloadType.List, PayloadList.Count, AbortVote, Exception);
