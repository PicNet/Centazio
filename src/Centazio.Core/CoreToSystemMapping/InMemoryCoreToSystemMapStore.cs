using System.Diagnostics.CodeAnalysis;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.CoreToSystemMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<Map.Key, string> memdb = new();
  
  public override Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    coreents.ForEach(c => {
      var json = memdb.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == CoreEntityTypeName.From(c) && kvp.Key.CoreId == c.CoreId && kvp.Key.System == system).Value;
      if (json is null) news.Add(new CoreAndPendingCreateMap(c, Map.Create(system, c)));
      else updates.Add(new CoreAndPendingUpdateMap(c, Deserialize(json).Update()));
    });
    return Task.FromResult((news, updates));
  }

  public override async Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    // todo: these validations should be done in base class
    var dudsysids = (await GetById(system, coretype, tocreate.Select(e => e.SystemId).ToList())).Select(m => m.SystemId).ToList();
    if (dudsysids.Any()) throw new Exception($"attempted to create duplicate CoreToSystemMaps [{system}/{coretype}] SystemIds[{String.Join(",", dudsysids)}]");
    var dupcoreids = (await GetById(system, coretype, tocreate.Select(e => e.CoreId).ToList())).Select(m => m.CoreId).ToList();
    if (dupcoreids.Any()) throw new Exception($"attempted to create duplicate CoreToSystemMaps [{system}/{coretype}] CoreIds[{String.Join(",", dupcoreids)}]");
    var nochecksums = tocreate.Where(e => String.IsNullOrWhiteSpace(e.SystemEntityChecksum.Value)).Select(e => e.SystemId.Value).ToList();
    if (nochecksums.Any()) throw new Exception($"attempted to create CoreToSystemMaps with no SystemEntityChecksum [{system}/{coretype}] Ids[{String.Join(",", nochecksums)}]");
    
    return tocreate.Select(map => {
      memdb[map.Key] = Json.Serialize(map);
      return map;
    }).ToList();
  }

  protected override Task<List<Map.CoreToSystemMap>> GetById<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysid = typeof(V) == typeof(SystemEntityId);
    var results = ids.Distinct()
        .Select(cid => Deserialize(memdb.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == coretype && (issysid ? kvp.Key.SystemId : kvp.Key.CoreId) == cid && kvp.Key.System == system).Value))
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(v => v != default)
        .ToList();
    return Task.FromResult(results);
  }

  public override Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    var missing = toupdate.Where(m => !memdb.ContainsKey(m.Key)).Select(m => m.SystemId).ToList();
    if (missing.Any()) throw new Exception($"attempted to update CoreToSystemMaps that do not exist [{system}/{coretype}] Ids[{String.Join(",", missing)}]");
    return Task.FromResult(toupdate.Select(map => {
      memdb[map.Key] = Json.Serialize(map);
      return map;
    }).ToList());
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
  
  [return: NotNullIfNotNull(nameof(json))]
  protected Map.CoreToSystemMap? Deserialize(string? json) => json is null ? null : Json.Deserialize<Map.CoreToSystemMap>(json);

}