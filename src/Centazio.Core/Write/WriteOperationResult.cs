namespace Centazio.Core.Write;

public delegate Task<List<Map.Created>> CreateEntitiesInExternalSystem<C, S>(List<CoreSystemAndPendingCreateMap<C, S>> tocreate) where C : ICoreEntity where S : ISystemEntity;
public delegate Task<List<Map.Updated>> UpdateEntitiesInExternalSystem<C, S>(List<CoreSystemAndPendingUpdateMap<C, S>> toupdate) where C : ICoreEntity where S : ISystemEntity;

public abstract record WriteOperationResult(
    List<Map.Created> EntitiesCreated,
    List<Map.Updated> EntitiesUpdated,
    EOperationResult Result,
    string Message,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue,
    Exception? Exception = null)
    : OperationResult(Result, Message, AbortVote, null, Exception), ILoggable {

  public int TotalChanges => EntitiesCreated.Count + EntitiesUpdated.Count;
  
  public string LoggableValue => $"EntitiesCreated[{EntitiesCreated.Count}] EntitiesUpdated[{EntitiesUpdated.Count}] Result[{Result}] Message[{Message}]";
  
  public static async Task<WriteOperationResult> Create<C, S>(
      List<CoreSystemAndPendingCreateMap> tocreate, 
      List<CoreSystemAndPendingUpdateMap> toupdate,
      CreateEntitiesInExternalSystem<C, S> creater,
      UpdateEntitiesInExternalSystem<C, S> updater) 
      where C : ICoreEntity where S : ISystemEntity {
    var created = tocreate.Any() ? await creater(tocreate.To<C, S>()) : [];
    var updated = toupdate.Any() ? await updater(toupdate.To<C, S>()) : [];
    return new SuccessWriteOperationResult(created, updated);
    
  }
}

internal sealed record SuccessWriteOperationResult(
    List<Map.Created> EntitiesCreated,
    List<Map.Updated> EntitiesUpdated,
    EOperationAbortVote AbortVote = EOperationAbortVote.Continue)
    : WriteOperationResult(EntitiesCreated,
        EntitiesUpdated,
        EOperationResult.Success,
        $"SuccessWriteOperationResult Created[{EntitiesCreated.Count}] Updated[{EntitiesUpdated.Count}]",
        AbortVote);
        