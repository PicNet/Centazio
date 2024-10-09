using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<Map.Key, Map.CoreToSystem> memdb = new();
  
  public override Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName system) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    cores.ForEach(c => {
      var obj = CoreEntityType.From(c);
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.System == system);
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, Map.Create(c, system)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult((news, updates));
  }
  
  public override Task<List<Map.CoreToSystem>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName system) => 
      Task.FromResult(coreids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.System == system);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<Map.CoreToSystem>()
          .ToList());
  
  public override Task<List<Map.CoreToSystem>> GetExistingMappingsFromSystemIds(CoreEntityType coretype, List<string> sysids, SystemName system) => 
      Task.FromResult(sysids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.SystemId == cid && k.System == system);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<Map.CoreToSystem>()
          .ToList());

  public override Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system) {
    var dict = potentialDups
        .Select(c => (c.SourceId, NewCoreId: memdb.Keys.SingleOrDefault(k => k.CoreEntity == CoreEntityType.From(c) && k.System == system && k.SystemId == c.SourceId)?.CoreId.Value))
        .Where(t => t.NewCoreId is not null)
        .ToDictionary(t => t.SourceId, t => t.NewCoreId!);
    return Task.FromResult(dict);
  }

  public override Task<List<Map.Created>> Create(CoreEntityType coretype, SystemName system, List<Map.Created> news) {
    if (!news.Any()) return Task.FromResult(new List<Map.Created>());
    
    // Log.Information("creating core/system maps {@CoreEntityType} {@System} {@CoreToSystemMapEntries}", coretype, system, news.Select(m => m.Key));
    var created = news.Select(map => {
      if (String.IsNullOrWhiteSpace(map.SystemEntityChecksum.Value)) throw new Exception(); 
      var duplicate = memdb.Keys.FirstOrDefault(k => k.CoreEntity == coretype && k.System == system && k.SystemId == map.SystemId);
      if (duplicate is not null) throw new Exception($"creating duplicate {nameof(Map.CoreToSystem)} map[{map}] existing[{duplicate}]");
      memdb[map.Key] = map;
      return map;
    }).ToList();
    return Task.FromResult(created);
  }

  public override Task<List<Map.Updated>> Update(CoreEntityType coretype, SystemName system, List<Map.Updated> updates) {
    if (!updates.Any()) return Task.FromResult(new List<Map.Updated>());
    
    // Log.Debug("updating core/system maps {@CoreEntityType} {@System} {@CoreToSystemMapEntries}", coretype, system, updates);
    updates.ForEach(map => memdb[map.Key] = map);
    return Task.FromResult(updates);
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}