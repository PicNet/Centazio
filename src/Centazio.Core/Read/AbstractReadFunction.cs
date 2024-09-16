using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract class AbstractReadFunction(IOperationsFilterAndPrioritiser<ReadOperationConfig>? prioritiser = null) 
    : AbstractFunction<ReadOperationConfig, ReadOperationResult>(prioritiser);
    
public record ReadOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> GetObjectsToStage) 
        : OperationConfig(Object, Cron, FirstTimeCheckpoint);
        
public abstract record ReadOperationResult(
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, AbortVote, Exception) {
  
  public bool IsValid { get; } = Result != EOperationResult.Unknown && Result != EOperationResult.Error && ResultLength > 0; 
}

public record ErrorReadOperationResult(string Message, EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) : ReadOperationResult(EOperationResult.Error, Message, EResultType.Error, 0, AbortVote, Exception);
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