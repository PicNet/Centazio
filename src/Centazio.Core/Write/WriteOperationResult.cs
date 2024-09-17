using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult<C>(
        ICollection<(C Core, EntityIntraSystemMapping Map)> EntitiesWritten,
        EOperationResult Result,
        string Message,
        EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
        Exception? Exception = null)
        : OperationResult(Result, Message, AbortVote, Exception)
        where C : ICoreEntity;

public record SuccessWriteOperationResult<C>(
    ICollection<(C Core, EntityIntraSystemMapping Map)> EntitiesWritten,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : WriteOperationResult<C>(
        EntitiesWritten, 
        EOperationResult.Success, 
        $"Write success results ({EntitiesWritten.Count})", 
        AbortVote) where C : ICoreEntity;

public record ErrorWriteOperationResult<C>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : WriteOperationResult<C>(
                Array.Empty<(C Core, EntityIntraSystemMapping Map)>(), 
                EOperationResult.Error, 
                $"Write error results[{Exception?.Message ?? "na"}] - abort[{AbortVote}]", 
                AbortVote, 
                Exception) where C : ICoreEntity;