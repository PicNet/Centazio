using System.Diagnostics.CodeAnalysis;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.CoreToSystemMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<Map.Key, string> memdb = new();
  
  protected override Task<List<Map.Created>> CreateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    var existingcoreids = memdb.Keys.Where(k => k.System == system && k.CoreEntityTypeName == coretype).ToDictionary(k => k.CoreId);
    var existingsysids = memdb.Keys.Where(k => k.System == system && k.CoreEntityTypeName == coretype).ToDictionary(k => k.SystemId);
    var results = tocreate.Select(map => {
      if (existingcoreids.ContainsKey(map.Key.CoreId) || existingsysids.ContainsKey(map.Key.SystemId)) return null;
      memdb[map.Key] = Json.Serialize(map);
      return map;
    }).Where(m => m is not null).Cast<Map.Created>().ToList();
    return Task.FromResult(results);
  }
  
  protected override Task<List<Map.Updated>> UpdateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    var results = toupdate.Select(map => {
      if (!memdb.ContainsKey(map.Key)) return null;
      memdb[map.Key] = Json.Serialize(map);
      return map;
    }).Where(m => m is not null).Cast<Map.Updated>().ToList();
    return Task.FromResult(results);
  }

  protected override Task<List<Map.CoreToSystemMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysid = typeof(V) == typeof(SystemEntityId);
    var results = ids.Distinct()
        .Select(cid => Deserialize(memdb.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == coretype && (issysid ? kvp.Key.SystemId : kvp.Key.CoreId) == cid && kvp.Key.System == system).Value))
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(v => v != default)
        .ToList();
    return Task.FromResult(results);
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
  
  [return: NotNullIfNotNull(nameof(json))]
  protected Map.CoreToSystemMap? Deserialize(string? json) => json is null ? null : Json.Deserialize<Map.CoreToSystemMap>(json);

}