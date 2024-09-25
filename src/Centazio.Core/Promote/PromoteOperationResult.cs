using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract record PromoteOperationResult(
        ICollection<(StagedEntity Staged, ICoreEntity Core)> ToPromote,
        ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore,
        EOperationResult Result,
        string Message,
        EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
        Exception? Exception = null)
        : OperationResult(Result, Message, AbortVote, Exception);

public record SuccessPromoteOperationResult(
    ICollection<(StagedEntity Staged, ICoreEntity Core)> ToPromote, 
    ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : PromoteOperationResult(
        ToPromote, 
        ToIgnore, 
        EOperationResult.Success, 
        $"SuccessPromoteOperationResult Promote[{ToPromote.Count}] Ignore[{ToIgnore.Count}]", 
        AbortVote);

public record ErrorPromoteOperationResult(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult(
                Array.Empty<(StagedEntity Staged, ICoreEntity Core)>(), 
                Array.Empty<(StagedEntity Entity, ValidString Reason)>(), 
                EOperationResult.Error, 
                $"ErrorPromoteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", 
                AbortVote, 
                Exception);