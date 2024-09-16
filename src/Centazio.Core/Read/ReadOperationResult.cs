using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result, 
    string Message, 
    EResultType ResultType, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, AbortVote, Exception) {
  
  public bool IsValid { get; } = Result != EOperationResult.Unknown && Result != EOperationResult.Error && ResultLength > 0; 
}

public record ErrorReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : ReadOperationResult(
                EOperationResult.Error, 
                $"read error results[{Exception?.Message ?? "na"}] - abort[{AbortVote}]", 
                EResultType.Error, 
                0, 
                AbortVote, 
                Exception);

public record EmptyReadOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success, 
        "read empty results", 
        EResultType.Empty, 
        0, 
        AbortVote);

public record SingleRecordReadOperationResult(ValidString Payload, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
        : ReadOperationResult(
            EOperationResult.Success, 
            $"read single item results[{Payload.Value.Length}]", 
            EResultType.Single, 
            Payload.Value.Length, AbortVote);

public record ListRecordsReadOperationResult(IReadOnlyList<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success, 
        $"read list results[{PayloadList.Count}]", 
        EResultType.List, 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote) {
  public IReadOnlyList<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}