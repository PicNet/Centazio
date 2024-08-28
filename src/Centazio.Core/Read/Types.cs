using centazio.core.Ctl.Entities;
using Cronos;

namespace Centazio.Core.Func;

public record ReadFunctionConfig(SystemName System, LifecycleStage Stage, List<ReadOperationConfig> Operations) {

  public void Validate() {
    if (!Operations.Any()) throw new Exception($"System {System} Read configuration has no operations defined"); 
  }
}

public record ReadOperationConfig(ObjectName Object, ValidCron Cron, Func<DateTime, ReadOperationStateAndConfig, Task<ReadOperationResult>> Impl);

public record ValidCron(string Expression) {
  public readonly CronExpression Value = CronExpression.Parse(Expression.Trim());
}

public record ReadOperationStateAndConfig(ObjectState State, ReadOperationConfig Settings);

public abstract record ReadOperationResult(EOperationReadResult Result, string Message, EPayloadType PayloadType, int PayloadLength, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) {
  public ReadOperationResult Validate() {
    if (PayloadType != EPayloadType.Empty && PayloadLength == 0) throw new Exception($"When a ReadOperationResult is not EPayloadType.Empty then the PayloadLength should be greater than 0");
    if (PayloadType == EPayloadType.Empty && PayloadLength != 0) throw new Exception($"When a ReadOperationResult is EPayloadType.Empty then the PayloadLength should be 0");
    if (Result == EOperationReadResult.Unknown) throw new Exception("ReadOperationResult should never be 'Unknown'");
    if (PayloadType == EPayloadType.List && this is not ListRecordReadOperationResult) throw new Exception("When a ReadOperationResult is of PayloadType=List then the result object should be of type ListRecordReadOperationResult");
    if (PayloadType == EPayloadType.Single && this is not SingleRecordReadOperationResult) throw new Exception("When a ReadOperationResult is of PayloadType=Single then the result object should be of type SingleRecordReadOperationResult");
    if (PayloadType == EPayloadType.Empty && this is not EmptyReadOperationResult) throw new Exception("When a ReadOperationResult is of PayloadType=Empty then the result object should be of type EmptyReadOperationResult");
    if (this is ListRecordReadOperationResult lr && !lr.PayloadList.Any()) throw new Exception("When a ReadOperationResult is of PayloadType=List then the PayloadList must have items");
    if (this is SingleRecordReadOperationResult sr && String.IsNullOrWhiteSpace(sr.Payload)) throw new Exception("When a ReadOperationResult is of PayloadType=Single then the Payload must have a valid string");
    return this;
  }
}

public record EmptyReadOperationResult(EOperationReadResult Result, string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.Empty, 0, AbortVote, Exception);

public record SingleRecordReadOperationResult(EOperationReadResult Result, string Message, string Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.Single, Payload.Length, AbortVote, Exception);

public record ListRecordReadOperationResult(EOperationReadResult Result, string Message, List<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
    : ReadOperationResult(Result, Message, EPayloadType.List, PayloadList.Count, AbortVote, Exception);
