﻿using Centazio.Core;
using centazio.core.Ctl.Entities;

namespace centazio.core.Ctl;

public class InMemoryCtlRepository(IUtcDate utc) : ICtlRepository {

  internal readonly Dictionary<(SystemName, LifecycleStage), SystemState> systems = new();
  internal readonly Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> objects = new();
  
  public Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) 
      => Task.FromResult(systems.GetValueOrDefault((system, stage)));
  
  public Task<SystemState> SaveSystemState(SystemState state) {
    var key = (state.System, state.Stage);
    if (!systems.ContainsKey(key)) throw new Exception($"SystemState[{state}] not found");
    return Task.FromResult(systems[key] = state);
  }
  
  public Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    var key = (system, stage);
    if (systems.ContainsKey(key)) throw new Exception($"Could not create SystemState[{key}] as it already exists in the CtlRepository");
    return Task.FromResult(systems[key] = new SystemState(system, stage, true, utc.Now));
  }

  public Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj)
      => Task.FromResult(objects.GetValueOrDefault((system.System, system.Stage, obj)));
  
  public Task<ObjectState> SaveObjectState(ObjectState state) {
    var key = (state.System, state.Stage, state.Object);
    if (!objects.ContainsKey(key)) throw new Exception($"ObjectState[{state}] not found");
    return Task.FromResult(objects[key] = state with { DateUpdated = utc.Now });
  }
  
  public Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj) {
    if (!systems.ContainsKey((system.System, system.Stage))) throw new Exception($"SystemState[{system.System}/{system.Stage}] does not exist.  Cannot create a child ObjectState");
    var key = (system.System, system.Stage, obj);
    if (objects.ContainsKey(key)) throw new Exception($"Could not create ObjectState[{key}] as it already exists in the CtlRepository");
    return Task.FromResult(objects[key] = new ObjectState(system.System, system.Stage, obj, true, utc.Now));
  }

  public ValueTask DisposeAsync() {
    systems.Clear();
    objects.Clear();
    return ValueTask.CompletedTask;
  }

}