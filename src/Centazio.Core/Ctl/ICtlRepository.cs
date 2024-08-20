using Centazio.Core;
using centazio.core.Ctl.Entities;

namespace centazio.core.Ctl;

public interface ICtlRepository {

  Task<SystemState?> GetSystemState(SystemName system);
  Task<SystemState> SaveSystemState(SystemState state);
  Task<ObjectState?> GetObjectState(SystemName system, ObjectName obj);
  Task<ObjectState> CreateObjectState(SystemName system, ObjectName obj);

  async Task<ObjectState> GetOrCreateObjectState(SystemName system, ObjectName obj) => 
      await GetObjectState(system, obj) ?? await CreateObjectState(system, obj);

  Task<ObjectState> SaveObjectState(ObjectState state);

}