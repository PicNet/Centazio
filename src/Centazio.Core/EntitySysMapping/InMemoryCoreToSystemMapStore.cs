using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<CoreToExternalMap.MappingKey, CoreToExternalMap> memdb = new();
  public override Task<List<CoreToExternalMap>> GetAll() => Task.FromResult(memdb.Values.ToList());
  public override Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key) => Task.FromResult(memdb[key]);

  // todo: remove `CoreEntityType obj` and use `CoreEntityType.From(c)`
  public override Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj) {
    var news = new List<CoreAndPendingCreateMap>();
    var updates = new List<CoreAndPendingUpdateMap>();
    cores.ForEach(c => {
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.ExternalSystem == target);
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, CoreToExternalMap.Create(c, target, obj)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult(news, updates));
  }
  
  // todo: this now seems redundant and same as `GetForCores`
  public override Task<List<CoreToExternalMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids) {
    return Task.FromResult(
        coreids.Select(cid => {
          var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.ExternalSystem == target);
          return key is null ? null : memdb[key];
        })
        .Where(m => m is not null)
        .Cast<CoreToExternalMap>()
        .ToList());
  }
  
  public override Task<string?> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys) {
    var coreid = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.ExternalSystem == externalsys && k.ExternalId == externalid)?.CoreId.Value;
    return Task.FromResult(coreid);
  }

  public override Task<List<CoreToExternalMap.Created>> Create(List<CoreToExternalMap.Created> news) {
    return Task.FromResult(news.Select(map => (CoreToExternalMap.Created)(memdb[map.Key] = map)).ToList());
  }

  public override Task<List<CoreToExternalMap.Updated>> Update(List<CoreToExternalMap.Updated> updates) {
    return Task.FromResult(updates.Select(map => (CoreToExternalMap.Updated)(memdb[map.Key] = map)).ToList());
  }
  
  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}