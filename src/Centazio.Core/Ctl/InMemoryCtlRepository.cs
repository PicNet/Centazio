using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Ctl;

public class InMemoryObjectStateRepository<O> : IObjectStateRepo<O> where O : ObjectName {

  internal readonly Dictionary<(SystemName, LifecycleStage, O), ObjectState<O>> objects = new();
  
  public Task<ObjectState<O>?> GetObjectState(SystemState system, O obj) => 
      Task.FromResult(objects.GetValueOrDefault((system.System, system.Stage, obj)));
  
  public Task<ObjectState<O>> SaveObjectState(ObjectState<O> state) {
    var key = (state.System, state.Stage, state.Object);
    if (!objects.ContainsKey(key)) throw new Exception($"ObjectState [{state}] not found");
    return Task.FromResult(objects[key] = state);
  }
  
  public Task<ObjectState<O>> CreateObjectState(SystemState system, O obj) {
    var key = (system.System, system.Stage, obj);
    if (objects.ContainsKey(key)) throw new Exception($"ObjectState [{key}] already exists");
    var os = new ObjectState<O>(system.System, system.Stage, obj, true);
    return Task.FromResult(objects[key] = os);
  }


  public ValueTask DisposeAsync() {
    objects.Clear();
    return ValueTask.CompletedTask;
  }

}

public class InMemoryCtlRepository : ICtlRepository {

  internal readonly Dictionary<(SystemName, LifecycleStage), SystemState> systems = new();
  internal readonly Dictionary<Type, object> objrepos = new();
  
  
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
  
  public IObjectStateRepo<O> GetObjectStateRepo<O>() where O : ObjectName => objrepos.TryGetValue(typeof(O), out var objrepo) 
      ? (IObjectStateRepo<O>) objrepo 
      : (IObjectStateRepo<O>) (objrepos[typeof(O)] = new InMemoryObjectStateRepository<O>());

  public ValueTask DisposeAsync() {
    systems.Clear();
    objrepos.Clear();
    return ValueTask.CompletedTask;
  }

}