using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;

namespace Centazio.Core.CoreToSystemMapping;

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate);
  Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents);
  Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids);
  
  Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedEntitySystemIdsFromCoreEntities(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey);
  Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedEntityCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public async Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    if (!tocreate.Any()) return [];
    ValidateMapsToUpsert(system, coretype, tocreate, true);
    var created = await CreateImpl(system, coretype, tocreate);
    if (created.Count != tocreate.Count) throw new Exception($"created maps({created.Count}) does not match expected number ({tocreate.Count}).  This chould mean that some pre-existing maps were attempted to be created.");
    return created;
  }
  
  public async Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    if (!toupdate.Any()) return [];
    ValidateMapsToUpsert(system, coretype, toupdate, false);
    var updated = await UpdateImpl(system, coretype, toupdate);
    if (updated.Count != toupdate.Count) throw new Exception($"updated maps({updated.Count}) does not match expected number ({toupdate.Count}).  This chould mean that some maps may not have existed already in the repository.");
    return updated;
  }

  private static void ValidateMapsToUpsert<M>(SystemName system, CoreEntityTypeName coretype, List<M> maps, bool iscreate) where M : Map.CoreToSystemMap {
    if (iscreate && maps.Any(e => e.System != system)) throw new ArgumentException($"All maps should have the same System[{system}]");
    if (maps.Any(e => e.CoreEntityTypeName != coretype)) throw new ArgumentException($"All maps should have the same CoreEntityTypeName[{system}]");
    if (iscreate && maps.Any(e => maps.First().DateCreated != e.DateCreated)) throw new ArgumentException($"All maps should have the same DateCreated[{system}]");
    if (maps.Any(e => maps.First().DateUpdated != e.DateUpdated)) throw new ArgumentException($"All maps should have the same DateUpdated[{system}]");
    if (maps.GroupBy(e => e.SystemId).Any(g => g.Count() > 1)) throw new ArgumentException($"All maps should be for unique system entities (have unique SystemIds)");
    if (maps.GroupBy(e => e.CoreId).Any(g => g.Count() > 1)) throw new ArgumentException($"All maps should be for unique core entities (have unique CoreIds)");
  }

  protected abstract Task<List<Map.Created>> CreateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate);
  protected abstract Task<List<Map.Updated>> UpdateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate);
  
  public async Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    if (!coreents.Any()) return (news, updates);
    ValidateCoreEntitiesForQuery(coretype, coreents, true);
    
    var maps = await GetExistingMapsByIds(system, coretype, coreents.Select(e => e.CoreId).ToList());
    coreents.ForEach(c => {
      var map = maps.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == CoreEntityTypeName.From(c) && kvp.Key.CoreId == c.CoreId && kvp.Key.System == system);
      if (map is null) news.Add(new CoreAndPendingCreateMap(c, Map.Create(system, c)));
      else updates.Add(new CoreAndPendingUpdateMap(c, map.Update()));
    });
    return (news, updates);
  }
  
  public Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult<List<Map.CoreToSystemMap>>([]);
    return GetExistingMapsByIds(system, coretype, coreids.Distinct().ToList());
  }

  public Task<List<Map.CoreToSystemMap>> GetExistingMappingsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids) {
    if (!systemids.Any()) return Task.FromResult<List<Map.CoreToSystemMap>>([]);
    return GetExistingMapsByIds(system, coretype, systemids.Distinct().ToList());
  }
  
  protected abstract Task<List<Map.CoreToSystemMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) where V : ValidString;
  
  public async Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedEntitySystemIdsFromCoreEntities(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey) {
    if (!coreents.Any()) return new();
    ValidateCoreEntitiesForQuery(coretype, coreents, false);
    
    var fks = coreents.Select(e => new CoreEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var maps = await GetExistingMappingsFromCoreIds(system, coretype, fks);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SystemId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {coretype} with CoreIds [{String.Join(",", missing)}]");
    
    return dict;
  }
  public async Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedEntityCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory) {
    if (!sysents.Any()) return new();
    if (sysents.Any(e => SystemEntityTypeName.From(e) != SystemEntityTypeName.From(sysents.First()))) throw new ArgumentException($"All system entities should be of the same SystemEntityTypeName[{system}]");
    if (sysents.GroupBy(e => e.SystemId).Any(g => g.Count() > 1)) throw new ArgumentException("found duplicate system entities (by SystemId)");
    
    var fks = sysents.Select(e => new SystemEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var dict = (await GetExistingMappingsFromSystemIds(system, coretype, fks)).ToDictionary(m => m.SystemId, m => m.CoreId);
    if (!mandatory) return dict;
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntityCoreIdsFromSystemIds[{system}] - Could not find {coretype} with SystemIds [{String.Join(",", missing)}]");
    return dict;
  }
  
  public abstract ValueTask DisposeAsync();
  
  private void ValidateCoreEntitiesForQuery(CoreEntityTypeName coretype, List<ICoreEntity> coreents, bool validatetype) {
    if (validatetype && coreents.Any(e => CoreEntityTypeName.From(e) != coretype)) throw new ArgumentException($"All core entities should match expected CoreEntityTypeName[{coretype}]");
    if (coreents.Any(e => CoreEntityTypeName.From(e) != CoreEntityTypeName.From(coreents.First()))) throw new ArgumentException($"All core entities should be of the same CoreEntityTypeName[{CoreEntityTypeName.From(coreents.First())}]");
    if (coreents.GroupBy(e => e.CoreId).Any(g => g.Count() > 1)) throw new ArgumentException("found duplicate core entities (by CoreId)");
    if (coreents.GroupBy(e => e.SystemId).Any(g => g.Count() > 1)) throw new ArgumentException("found duplicate core entities (by SystemId)");
  }

}