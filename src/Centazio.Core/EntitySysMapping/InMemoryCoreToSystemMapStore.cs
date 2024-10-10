using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<Map.Key, Map.CoreToSystem> memdb = new();
  
  public override Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> cores) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    cores.ForEach(c => {
      var existing = memdb.SingleOrDefault(kvp => kvp.Key.CoreEntityType == CoreEntityType.From(c) && kvp.Key.CoreId == c.Id && kvp.Key.System == system).Value;
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, Map.Create(system, c)));
      else updates.Add(new CoreAndPendingUpdateMap(c, existing.Update()));
    });
    return Task.FromResult((news, updates));
  }
  
  public override Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityType coretype, List<CoreEntityId> coreids) => 
      Task.FromResult(GetById(system, coretype, coreids));
  
  public override Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityType coretype, List<SystemEntityId> sysids) => 
      Task.FromResult(GetById(system, coretype, sysids));

  public override Task<Dictionary<SystemEntityId, CoreEntityId>> GetPreExistingSourceIdToCoreIdMap(SystemName system, CoreEntityType coretype, List<ICoreEntity> entities) {
    var lst = GetById(system, coretype, entities.Select(e => e.SourceId).ToList());
    return Task.FromResult(lst.ToDictionary(m => m.SystemId, m => m.CoreId));
  }

  public override Task<List<Map.Created>> Create(SystemName system, CoreEntityType coretype, List<Map.Created> news) {
    var duplicates = GetById(system, coretype, news.Select(e => e.SystemId).ToList()).Select(m => m.SystemId).ToList();
    if (duplicates.Any()) throw new Exception($"attempted to create duplicate CoreToSystemMaps [{system}/{coretype}] Ids[{String.Join(",", duplicates)}]");
    var nochecksums = news.Where(e => String.IsNullOrWhiteSpace(e.SystemEntityChecksum.Value)).Select(e => e.SystemId.Value).ToList();
    if (nochecksums.Any()) throw new Exception($"attempted to create CoreToSystemMaps with no SystemEntityChecksum [{system}/{coretype}] Ids[{String.Join(",", nochecksums)}]");
    
    return Task.FromResult(news.Select(map => memdb[map.Key] = map).Cast<Map.Created>().ToList());
  }

  private List<Map.CoreToSystem> GetById<V>(SystemName system, CoreEntityType coretype, List<V> ids) where V : ValidString {
    var issysid = typeof(V) == typeof(SystemEntityId);
    return ids.Distinct()
        .Select(cid => memdb.SingleOrDefault(kvp => kvp.Key.CoreEntityType == coretype && (issysid ? kvp.Key.SystemId : kvp.Key.CoreId) == cid && kvp.Key.System == system).Value)

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        .Where(v => v != default)
        .ToList();
  }

  public override Task<List<Map.Updated>> Update(SystemName system, CoreEntityType coretype, List<Map.Updated> updates) {
    var missing = updates.Where(m => !memdb.ContainsKey(m.Key)).Select(m => m.SystemId).ToList();
    if (missing.Any()) throw new Exception($"attempted to update CoreToSystemMaps that do not exist [{system}/{coretype}] Ids[{String.Join(",", missing)}]");
    return Task.FromResult(updates.Select(map => memdb[map.Key] = map).Cast<Map.Updated>().ToList());
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}