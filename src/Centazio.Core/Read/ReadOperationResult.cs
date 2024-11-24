﻿using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract record ReadOperationResult(
    EOperationResult Result, 
    string Message, 
    int ResultLength, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null) : OperationResult(Result, Message, AbortVote, Exception), ILoggable {
    
  public static ReadOperationResult Create(string value) => String.IsNullOrWhiteSpace(value) ? new EmptyReadOperationResult() : new SingleRecordReadOperationResult(new (value));
  public static ReadOperationResult Create(List<string> lst) => !lst.Any() ? new EmptyReadOperationResult() : new ListRecordsReadOperationResult(lst);
  
  public string LoggableValue => $"{Result} -> {ResultLength} Message[{Message}]";

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

public record ListRecordsReadOperationResult(List<string> PayloadList, EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : ReadOperationResult(
        EOperationResult.Success, 
        $"ListRecordsReadOperationResult[{PayloadList.Count}]", 
        PayloadList.Any() ? PayloadList.Count : throw new ArgumentNullException(), 
        AbortVote) {
  public List<string> PayloadList { get; } = PayloadList.Any() && !PayloadList.Any(String.IsNullOrWhiteSpace) 
      ? PayloadList : throw new ArgumentNullException(nameof(PayloadList));
}