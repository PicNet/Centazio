using System.Diagnostics.CodeAnalysis;

namespace Centazio.Test.Lib.InMemRepos;

public record SystemStateKey(SystemName System, LifecycleStage Stage) {
  public override string ToString() => $"{System.Value}/{Stage.Value}";
}

public record ObjectStateKey(SystemName System, LifecycleStage Stage, ObjectName Object) {
  public override string ToString() => $"{System.Value}/{Stage.Value}/{Object.Value}";
}

public class InMemoryBaseCtlRepository : AbstractCtlRepository {

  protected readonly Dictionary<SystemStateKey, SystemState> systems = new();
  protected readonly Dictionary<ObjectStateKey, ObjectState> objects = new();
  protected readonly Dictionary<Map.Key, string> maps = new();
  protected readonly List<EntityChange> changes = new();
  
  public override Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) 
      => Task.FromResult(systems.GetValueOrDefault(new (system, stage)));
  
  public override Task<SystemState> SaveSystemState(SystemState state) {
    var key = new SystemStateKey(state.System, state.Stage);
    if (!systems.ContainsKey(key)) throw new Exception($"SystemState [{state}] not found");
    return Task.FromResult(systems[key] = state);
  }

  public override Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    var key = new SystemStateKey(system, stage);
    if (systems.ContainsKey(key)) throw new Exception($"SystemState [{key}] already exists");
    return Task.FromResult(systems[key] = SystemState.Create(system, stage));
  }
  
  public override Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    var key = new ObjectStateKey(system.System, system.Stage, obj);
    return Task.FromResult(objects.GetValueOrDefault(key));
    
  }

  public override Task<ObjectState> SaveObjectState(ObjectState state) {
    var key = new ObjectStateKey(state.System, state.Stage, state.Object);
    if (!objects.ContainsKey(key)) throw new Exception($"ObjectState [{state}] not found");
    return Task.FromResult(objects[key] = state);
  }
  
  public override Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint) {
    var key = new ObjectStateKey(system.System, system.Stage, obj);
    if (objects.ContainsKey(key)) throw new Exception($"ObjectState [{key}] already exists");
    var state = new ObjectState(system.System, system.Stage, obj, nextcheckpoint, true);
    return Task.FromResult(objects[key] = state);
  }
  
  protected override Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    var existingcoreids = maps.Keys.Where(k => k.System == system && k.CoreEntityTypeName == coretype).ToDictionary(k => k.CoreId);
    var existingsysids = maps.Keys.Where(k => k.System == system && k.CoreEntityTypeName == coretype).ToDictionary(k => k.SystemId);
    var results = tocreate.Select(map => {
      if (existingcoreids.ContainsKey(map.Key.CoreId) || existingsysids.ContainsKey(map.Key.SystemId)) return null;
      maps[map.Key] = Json.Serialize(map);
      return map;
    }).Where(m => m is not null).Cast<Map.Created>().ToList();
    return Task.FromResult(results);
  }
  
  protected override Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    var results = toupdate.Select(map => {
      if (!maps.ContainsKey(map.Key)) return null;
      maps[map.Key] = Json.Serialize(map);
      return map;
    }).Where(m => m is not null).Cast<Map.Updated>().ToList();
    return Task.FromResult(results);
  }

  protected override Task SaveEntityChangesImpl(List<EntityChange> batch) {
    changes.AddRange(batch);
    return Task.CompletedTask;
  }

  protected override Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysent = typeof(V) == typeof(SystemEntityId);
    var results = ids.Distinct()
        .Select(cid => Deserialize(maps.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == coretype && (issysent ? kvp.Key.SystemId : kvp.Key.CoreId) == cid && kvp.Key.System == system).Value))
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(v => v is not null)
        .OrderBy(e => issysent ? e.SystemId.Value : e.CoreId.Value)
        .ToList();
    return Task.FromResult(results);
  }

  [return: NotNullIfNotNull(nameof(json))]
  protected Map.CoreToSysMap? Deserialize(string? json) => json is null ? null : Json.Deserialize<Map.CoreToSysMap>(json);

  public override Task<ICtlRepository> Initialise() => Task.FromResult<ICtlRepository>(this);
  public override ValueTask DisposeAsync() {
    systems.Clear();
    objects.Clear();
    maps.Clear();
    return ValueTask.CompletedTask;
  }

}