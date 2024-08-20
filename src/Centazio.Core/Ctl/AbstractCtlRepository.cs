using Centazio.Core;
using centazio.core.Ctl.Entities;

namespace centazio.core.Ctl;

public abstract class AbstractCtlRepository : ICtlRepository {

  public async Task<ObjectState> CreateObjectState(SystemName system, ObjectName obj) {
    if (await GetObjectState(system, obj) != null) throw new Exception($"Could not create ObjectState for {system.Value} / {obj.Value} as it already exists in the CtlRepository");
    var sysstate = await GetSystemState(system) ?? throw new Exception($"Could not create ObjectState for {system.Value} / {obj.Value} as the parent SystemState does not exist in the CtlRepository");
    return await SaveObjectState(new ObjectState(sysstate, obj, DateTime.UtcNow.AddYears(-100), null, null, 0, EOperationReadResult.Unknown, EOperationAbortVote.Unknown, null));
  }

  public abstract Task<SystemState?> GetSystemState(SystemName system);
  public abstract Task<SystemState> SaveSystemState(SystemState state);
  public abstract Task<ObjectState?> GetObjectState(SystemName system, ObjectName obj);
  public abstract Task<ObjectState> SaveObjectState(ObjectState state);

}