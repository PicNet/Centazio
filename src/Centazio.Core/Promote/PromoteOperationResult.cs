using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract record PromoteOperationResult<E>(
        ICollection<(StagedEntity Staged, E Core)> ToPromote,
        ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore,
        EOperationResult Result,
        string Message,
        EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
        Exception? Exception = null)
        : OperationResult(Result, Message, AbortVote, Exception)
        where E : ICoreEntity;

public record SuccessPromoteOperationResult<E>(
    ICollection<(StagedEntity Staged, E Core)> ToPromote, 
    ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : PromoteOperationResult<E>(
        ToPromote, 
        ToIgnore, 
        EOperationResult.Success, 
        $"SuccessPromoteOperationResult Promote[{ToPromote.Count}] Ignore[{ToIgnore.Count}]", 
        AbortVote) where E : ICoreEntity;

public record ErrorPromoteOperationResult<E>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult<E>(
                Array.Empty<(StagedEntity Staged, E Core)>(), 
                Array.Empty<(StagedEntity Entity, ValidString Reason)>(), 
                EOperationResult.Error, 
                $"ErrorPromoteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", 
                AbortVote, 
                Exception) where E : ICoreEntity;