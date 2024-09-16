using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract record PromoteOperationResult<C>(
        ICollection<(StagedEntity Staged, C Core)> ToPromote,
        ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore,
        EOperationResult Result,
        string Message,
        EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
        Exception? Exception = null)
        : OperationResult(Result, Message, AbortVote, Exception)
        where C : ICoreEntity;

public record SuccessPromoteOperationResult<C>(
    ICollection<(StagedEntity Staged, C Core)> ToPromote, 
    ICollection<(StagedEntity Entity, ValidString Reason)> ToIgnore, 
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : PromoteOperationResult<C>(
        ToPromote, 
        ToIgnore, 
        EOperationResult.Success, 
        $"promote success results ({ToPromote.Count}/{ToIgnore.Count})", 
        AbortVote) where C : ICoreEntity;

public record ErrorPromoteOperationResult<C>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : PromoteOperationResult<C>(
                Array.Empty<(StagedEntity Staged, C Core)>(), 
                Array.Empty<(StagedEntity Entity, ValidString Reason)>(), 
                EOperationResult.Error, 
                $"promote error results[{Exception?.Message ?? "na"}] - abort[{AbortVote}]", 
                AbortVote, 
                Exception) where C : ICoreEntity;