using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;
using Serilog;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryCoreToSystemMapStore : AbstractCoreToSystemMapStore {

  protected readonly Dictionary<CoreToExternalMap.MappingKey, CoreToExternalMap> memdb = new();
  public override Task<List<CoreToExternalMap>> GetAll() => Task.FromResult(memdb.Values.ToList());

  // todo: rename these overrides, they do not share return types
  public override Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName external) {
    if (cores.Any()) DevelDebug.WriteLine($"GetForCores1 CoreIds[{String.Join(';', cores.Select(c => c.Id))}] Type[{CoreEntityType.From(cores.First())}] External[{external}]");
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
    DevelDebug.WriteLine($"GetForCores2 CoreIds[{String.Join(",", coreids)}] Type[{coretype}] External[{external}]");
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
    if (!news.Any()) return Task.FromResult(new List<CoreToExternalMap.Created>());
    
    Log.Information("Creating {@CoreToSysMaps}", news);
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

  private void ValidateDuplicates() {
    var keys = memdb.Keys;
    var dulpicates = keys.GroupBy(k => new { k.CoreEntity, k.ExternalSystem, k.ExternalId } ).Where(g => g.Count() > 1).Select(g => $"{String.Join(",", g.ToList())}: {g.Count()}").ToList();
    if (dulpicates.Any()) throw new Exception($"found duplicate external entities in CoreToSysMap:\n\t{String.Join("\n\t", dulpicates)}");
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}