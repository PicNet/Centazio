using Centazio.Core;
using centazio.core.Ctl.Entities;

namespace centazio.core.Ctl;

public class InMemoryCtlRepository : AbstractCtlRepository {

  private readonly Dictionary<SystemName, SystemState> systems = new();
  private readonly Dictionary<(SystemName, ObjectName), ObjectState> objects = new();
  
  public override Task<SystemState?> GetSystemState(SystemName system) => Task.FromResult(systems.GetValueOrDefault(system));
  public override Task<SystemState> SaveSystemState(SystemState state) => Task.FromResult(systems[state.System] = state);
  public override Task<ObjectState?> GetObjectState(SystemName system, ObjectName obj) => Task.FromResult(objects.GetValueOrDefault((system, obj)));
  public override Task<ObjectState> SaveObjectState(ObjectState state) => Task.FromResult(objects[(state.System.System, state.Object)] = state);

}