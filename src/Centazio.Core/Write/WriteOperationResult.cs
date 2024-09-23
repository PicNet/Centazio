using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public abstract record WriteOperationResult<E>(
    ICollection<(E Core, EntityIntraSysMap.Created Map)> EntitiesCreated,
    ICollection<(E Core, EntityIntraSysMap.Updated Map)> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, Exception)
        where E : ICoreEntity {
  
  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;
}

public record SuccessWriteOperationResult<E>(
    ICollection<(E Core, EntityIntraSysMap.Created Map)> EntitiesCreated,
    ICollection<(E Core, EntityIntraSysMap.Updated Map)> EntitiesUpdated,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue) 
    : WriteOperationResult<E>(
        EntitiesCreated, 
        EntitiesUpdated,
        EOperationResult.Success, 
        $"SuccessWriteOperationResult Created[{EntitiesCreated.Count}] Updated[{EntitiesUpdated.Count}]",
        AbortVote) where E : ICoreEntity;

public record ErrorWriteOperationResult<E>(EOperationAbortVote AbortVote = EOperationAbortVote.Continue, Exception? Exception = null) 
        : WriteOperationResult<E>(
                Array.Empty<(E Core, EntityIntraSysMap.Created Map)>(),
                Array.Empty<(E Core, EntityIntraSysMap.Updated Map)>(),
                EOperationResult.Error, 
                $"ErrorWriteOperationResult[{Exception?.Message ?? "na"}] - AbortVote[{AbortVote}]", 
                AbortVote, 
                Exception) where E : ICoreEntity;