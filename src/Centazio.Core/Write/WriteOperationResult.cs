using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult<E>(
        ICollection<(E Core, EntityIntraSystemMapping Map)> EntitiesWritten,
        EOperationResult Result,
        string Message,
        EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
        Exception? Exception = null)
        : OperationResult(Result, Message, AbortVote, Exception)
        where E : ICoreEntity;

public record SuccessWriteOperationResult<E>(
    ICollection<(E Core, EntityIntraSystemMapping Map)> EntitiesWritten,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : WriteOperationResult<E>(
        EntitiesWritten, 
        EOperationResult.Success, 
        $"Write success results ({EntitiesWritten.Count})", 
        AbortVote) where E : ICoreEntity;

public record ErrorWriteOperationResult<E>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : WriteOperationResult<E>(
                Array.Empty<(E Core, EntityIntraSystemMapping Map)>(), 
                EOperationResult.Error, 
                $"Write error results[{Exception?.Message ?? "na"}] - abort[{AbortVote}]", 
                AbortVote, 
                Exception) where E : ICoreEntity;