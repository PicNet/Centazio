using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Ctl;

public class InMemoryCtlRepository : ICtlRepository {

  internal readonly Dictionary<(SystemName, LifecycleStage), SystemState> systems = new();
  internal readonly Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectStateDto> objects = new();
  
  public Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) 
      => Task.FromResult(systems.GetValueOrDefault((system, stage)));
  
  public Task<SystemState> SaveSystemState(SystemState state) {
    var key = (state.System, state.Stage);
    if (!systems.ContainsKey(key)) throw new Exception($"SystemState [{state}] not found");
    return Task.FromResult(systems[key] = state);
  }
  
  public Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    var key = (system, stage);
    if (systems.ContainsKey(key)) throw new Exception($"SystemState [{key}] already exists");
    return Task.FromResult(systems[key] = SystemState.Create(system, stage));
  }

  public Task<ObjectState<T>?> GetObjectState<T>(SystemState system, T obj) where T : ObjectName => 
      Task.FromResult(objects.GetValueOrDefault((system.System, system.Stage, obj))?.ToObjectState<T>());
  
  public Task<ObjectState<T>> SaveObjectState<T>(ObjectState<T> state) where T : ObjectName {
    var key = (state.System, state.Stage, state.Object);
    if (!objects.ContainsKey(key)) throw new Exception($"ObjectState [{state}] not found");
    objects[key] = ObjectStateDto.FromObjectState(state);
    return Task.FromResult(state);
  }
  
  public Task<ObjectState<T>> CreateObjectState<T>(SystemState system, T obj) where T : ObjectName {
    if (!systems.ContainsKey((system.System, system.Stage))) throw new Exception($"SystemState [{system}] does not exist");
    var key = (system.System, system.Stage, obj);
    if (objects.ContainsKey(key)) throw new Exception($"ObjectState [{key}] already exists");
    var os = new ObjectState<T>(system.System, system.Stage, obj, true);
    objects[key] = ObjectStateDto.FromObjectState(os);
    return Task.FromResult(os);
  }

  public ValueTask DisposeAsync() {
    systems.Clear();
    objects.Clear();
    return ValueTask.CompletedTask;
  }

}