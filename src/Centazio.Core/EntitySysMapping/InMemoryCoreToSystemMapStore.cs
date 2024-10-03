using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<CoreToExternalMap.MappingKey, CoreToExternalMap> memdb = new();
  
  public override Task<GetForCoresResult> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName external) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    cores.ForEach(c => {
      var obj = CoreEntityType.From(c);
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.ExternalSystem == external);
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, CoreToExternalMap.Create(c, external)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult(news, updates));
  }
  
  public override Task<List<CoreToExternalMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName external) => 
      Task.FromResult(coreids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.ExternalSystem == external);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<CoreToExternalMap>()
          .ToList());

  // var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.ExternalSystem == externalsys && k.ExternalId == externalid);
  // return Task.FromResult(key?.CoreId.Value);
  public override Task<List<CoreToExternalMap>> GetExistingMappingsFromExternalIds(CoreEntityType coretype, List<string> coreids, SystemName external) => 
      Task.FromResult(coreids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.ExternalId == cid && k.ExternalSystem == external);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<CoreToExternalMap>()
          .ToList());

  public override Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system) {
    var dict = potentialDups
        .Select(c => (c.SourceId, NewCoreId: memdb.Keys.SingleOrDefault(k => k.CoreEntity == CoreEntityType.From(c) && k.ExternalSystem == system && k.ExternalId == c.SourceId)?.CoreId.Value))
        .Where(t => t.NewCoreId is not null)
        .ToDictionary(t => t.SourceId, t => t.NewCoreId!);
    return Task.FromResult(dict);
  }

  public override Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> news) {
    if (!news.Any()) return Task.FromResult(new List<CoreToExternalMap.Created>());
    
    // Log.Information("Creating {@CoreToSysMaps}", news);
    var created = news.Select(map => {
      var duplicate = memdb.Keys.FirstOrDefault(k => k.CoreEntity == map.CoreEntity && k.ExternalSystem == map.ExternalSystem && k.ExternalId == map.ExternalId);
      if (duplicate is not null) throw new Exception($"creating duplicate CoreToExternalMap map[{map}] existing[{duplicate}]");
      memdb[map.Key] = map;
      return map;
    }).ToList();
    return Task.FromResult(created);
  }

  public override Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> updates) {
    if (!updates.Any()) return Task.FromResult(new List<CoreToExternalMap.Updated>());
    
    var updated = updates.Select(map => (CoreToExternalMap.Updated)(memdb[map.Key] = map)).ToList();
    return Task.FromResult(updated);
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}