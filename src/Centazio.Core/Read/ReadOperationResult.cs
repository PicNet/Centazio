using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result, 
    string Message, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, AbortVote, Exception) {
  
  public static ReadOperationResult Create(List<string> lst) {
    if (!lst.Any()) return new EmptyReadOperationResult();
    return new ListRecordsReadOperationResult(lst);
  }
}

public record ErrorReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : ReadOperationResult(
                EOperationResult.Error, 
                $"ErrorReadOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", 
                0, 
                AbortVote, 
                Exception);

public record EmptyReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success, 
        "EmptyReadOperationResult", 
        0, 
        AbortVote);

public record SingleRecordReadOperationResult(ValidString Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
        : ReadOperationResult(
            EOperationResult.Success, 
            $"SingleRecordReadOperationResult[{Payload.Value.Length}]", 
            Payload.Value.Length, AbortVote);

public record ListRecordsReadOperationResult(IReadOnlyList<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success, 
        $"ListRecordsReadOperationResult[{PayloadList.Count}]", 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote) {
  public IReadOnlyList<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}