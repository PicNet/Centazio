using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;
using Serilog;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<CoreToSystemMap.MappingKey, CoreToSystemMap> memdb = new();
  
  public override Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName external) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    cores.ForEach(c => {
      var obj = CoreEntityType.From(c);
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.ExternalSystem == external);
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, CoreToSystemMap.Create(c, external)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult((news, updates));
  }
  
  public override Task<List<CoreToSystemMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName external) => 
      Task.FromResult(coreids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.ExternalSystem == external);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<CoreToSystemMap>()
          .ToList());
  
  public override Task<List<CoreToSystemMap>> GetExistingMappingsFromExternalIds(CoreEntityType coretype, List<string> externalids, SystemName external) => 
      Task.FromResult(externalids.Distinct().Select(cid => {
            var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.ExternalId == cid && k.ExternalSystem == external);
            return key is null ? null : memdb[key];
          })
          .Where(m => m is not null)
          .Cast<CoreToSystemMap>()
          .ToList());

  public override Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system) {
    var dict = potentialDups
        .Select(c => (c.SourceId, NewCoreId: memdb.Keys.SingleOrDefault(k => k.CoreEntity == CoreEntityType.From(c) && k.ExternalSystem == system && k.ExternalId == c.SourceId)?.CoreId.Value))
        .Where(t => t.NewCoreId is not null)
        .ToDictionary(t => t.SourceId, t => t.NewCoreId!);
    return Task.FromResult(dict);
  }

  public override Task<List<CoreToSystemMap.Created>> Create(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Created> news) {
    if (!news.Any()) return Task.FromResult(new List<CoreToSystemMap.Created>());
    
    Log.Information("creating core/external maps {@CoreEntityType} {@System} {@CoreToExternalMapEntries}", coretype, system, news.Select(m => m.ExternalId));
    var created = news.Select(map => {
      if (String.IsNullOrWhiteSpace(map.SystemEntityChecksum.Value)) throw new Exception(); 
      var duplicate = memdb.Keys.FirstOrDefault(k => k.CoreEntity == coretype && k.ExternalSystem == system && k.ExternalId == map.ExternalId);
      if (duplicate is not null) throw new Exception($"creating duplicate CoreToExternalMap map[{map}] existing[{duplicate}]");
      memdb[map.Key] = map;
      return map;
    }).ToList();
    return Task.FromResult(created);
  }

  public override Task<List<CoreToSystemMap.Updated>> Update(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Updated> updates) {
    if (!updates.Any()) return Task.FromResult(new List<CoreToSystemMap.Updated>());
    updates.ForEach(map => {
      // todo: need to clean up types and make null Checksums impossible, we have a mess of types in CoreToSystemMap.cs
      if (String.IsNullOrWhiteSpace(map.SystemEntityChecksum.Value)) throw new Exception();
      memdb[map.Key] = map;
    });
    return Task.FromResult(updates);
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}