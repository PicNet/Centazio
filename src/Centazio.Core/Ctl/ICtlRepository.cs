using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Ctl;

public interface ICtlRepository : IAsyncDisposable {

  Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> SaveSystemState(SystemState state);
  async Task<SystemState> GetOrCreateSystemState(SystemName system, LifecycleStage stage) => 
      await GetSystemState(system, stage) ?? await CreateSystemState(system, stage);
  
  Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj);
  Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj);
  Task<ObjectState> SaveObjectState(ObjectState state);
  
  async Task<ObjectState> GetOrCreateObjectState(SystemState system, ObjectName obj) => 
      await GetObjectState(system, obj) ?? await CreateObjectState(system, obj);
}