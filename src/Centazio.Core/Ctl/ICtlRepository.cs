using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Ctl;

public interface ICtlRepository : IAsyncDisposable {

  Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage);
  Task<SystemState> SaveSystemState(SystemState state);
  
  async Task<SystemState> GetOrCreateSystemState(SystemName system, LifecycleStage stage) => 
      await GetSystemState(system, stage) ?? await CreateSystemState(system, stage);
  
  IObjectStateRepo<O> GetObjectStateRepo<O>() where O : ObjectName;
}

public interface IObjectStateRepo<O> : IAsyncDisposable 
    where O : ObjectName {
  
  Task<ObjectState<O>?> GetObjectState(SystemState system, O obj);
  Task<ObjectState<O>> CreateObjectState(SystemState system, O obj);
  Task<ObjectState<O>> SaveObjectState(ObjectState<O> state);
  
  async Task<ObjectState<O>> GetOrCreateObjectState(SystemState system, O obj) => await GetObjectState(system, obj) ?? await CreateObjectState(system, obj);
}