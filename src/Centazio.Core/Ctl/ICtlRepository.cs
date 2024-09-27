using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Ctl;

public interface ICtlRepository : IAsyncDisposable {

  Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> SaveSystemState(SystemState state);
  
  Task<ObjectState<T>?> GetObjectState<T>(SystemState system, T obj) where T : ObjectName;
  Task<ObjectState<T>> CreateObjectState<T>(SystemState system, T obj) where T : ObjectName;
  Task<ObjectState<T>> SaveObjectState<T>(ObjectState<T> state) where T : ObjectName;
  
  // default implementations
  
  async Task<SystemState> GetOrCreateSystemState(SystemName system, LifecycleStage stage) => 
      await GetSystemState(system, stage) ?? await CreateSystemState(system, stage);
  
  async Task<ObjectState<T>> GetOrCreateObjectState<T>(SystemState system, T obj) where T : ObjectName => 
      await GetObjectState(system, obj) ?? await CreateObjectState(system, obj);
}