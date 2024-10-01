using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<CoreToExternalMap.MappingKey, CoreToExternalMap> memdb = new();
  public override Task<List<CoreToExternalMap>> GetAll() => Task.FromResult(memdb.Values.ToList());
  public override Task<CoreToExternalMap> GetSingle(CoreToExternalMap.MappingKey key) => Task.FromResult(memdb[key]);

  public override Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    cores.ForEach(c => {
      var obj = CoreEntityType.From(c);
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.ExternalSystem == external);
      if (existing is null) news.Add(new CoreAndPendingCreateMap(c, CoreToExternalMap.Create(c, external)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult(news, updates));
  }
  
  public override Task<List<CoreToExternalMap>> GetForCores(CoreEntityType coretype, List<string> coreids, SystemName external) {
    return Task.FromResult(
        coreids.Distinct().Select(cid => {
          var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.ExternalSystem == external);
          return key is null ? null : memdb[key];
        })
        .Where(m => m is not null)
        .Cast<CoreToExternalMap>()
        .ToList());
  }
  
  public override Task<string> GetCoreIdForSystem(CoreEntityType obj, string externalid, SystemName externalsys) {
    return Task.FromResult(memdb.Keys.Single(k => k.CoreEntity == obj && k.ExternalSystem == externalsys && k.ExternalId == externalid).CoreId.Value);
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