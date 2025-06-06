﻿namespace Centazio.Core.Ctl;

public abstract class AbstractCtlRepository : ICtlRepository {

  public abstract Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null);
  
  public abstract Task<ICtlRepository> Initialise();
  
  public abstract Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage);
  public abstract Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage);
  public abstract Task<SystemState> SaveSystemState(SystemState state);
  
  public abstract Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj);
  public abstract Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj, DateTime nextcheckpoint);
  public abstract Task<ObjectState> SaveObjectState(ObjectState state);
  
  public async Task<SystemState> GetOrCreateSystemState(SystemName system, LifecycleStage stage) => await GetSystemState(system, stage) ?? await CreateSystemState(system, stage);
  public async Task<ObjectState> GetOrCreateObjectState(SystemState system, ObjectName obj, DateTime firstcheckpoint) => await GetObjectState(system, obj) ?? await CreateObjectState(system, obj, firstcheckpoint);
  
  protected abstract Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate);
  protected abstract Task<List<Map.Updated>> UpdateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate);
  protected abstract Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) where V : ValidString;
  
  protected abstract Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> batch);
  public abstract Task<List<EntityChange>> GetEntityChanges(CoreEntityTypeName coretype, DateTime after);
  public abstract Task<List<EntityChange>> GetEntityChanges(SystemName system, SystemEntityTypeName systype, DateTime after);
  
  public abstract ValueTask DisposeAsync();
  
  public async Task<List<Map.Created>> CreateSysMap(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    if (!tocreate.Any()) return [];
    ValidateMapsToUpsert(system, coretype, tocreate, true);
    var created = await CreateMapImpl(system, coretype, tocreate);
    if (created.Count != tocreate.Count) throw new Exception($"created maps({created.Count}) does not match expected number ({tocreate.Count}).  This chould mean that some pre-existing maps were attempted to be created.");
    return created;
  }
  
  public async Task<List<Map.Updated>> UpdateSysMap(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    if (!toupdate.Any()) return [];
    ValidateMapsToUpsert(system, coretype, toupdate, false);
    var updated = await UpdateMapImpl(system, coretype, toupdate);
    if (updated.Count != toupdate.Count) throw new Exception($"updated maps({updated.Count}) does not match expected number ({toupdate.Count}).  This chould mean that some maps may not have existed already in the repository.");
    return updated;
  }

  public async Task<List<EntityChange>> SaveEntityChanges(List<EntityChange> changes) {
    if (!changes.Any()) return [];
    return await SaveEntityChangesImpl(changes);
  }

  private static void ValidateMapsToUpsert<M>(SystemName system, CoreEntityTypeName coretype, List<M> maps, bool iscreate) where M : Map.CoreToSysMap {
    if (iscreate) { ValidateAllMapsHaveSame("System", system, e => e.System); }
    ValidateAllMapsHaveSame("CoreEntityTypeName", coretype, e => e.CoreEntityTypeName);
    
    if (maps.GroupBy(e => e.SystemId).Any(g => g.Count() > 1)) throw new ArgumentException($"All maps should be for unique system entities (have unique SystemIds)");
    if (maps.GroupBy(e => e.CoreId).Any(g => g.Count() > 1)) throw new ArgumentException($"All maps should be for unique core entities (have unique CoreIds)");
    
    void ValidateAllMapsHaveSame<T>(string field, T exp, Func<M, T> actual) {
      var first = maps.FirstOrDefault(e => {
        var val = actual(e) ?? throw new Exception();
        return !val.Equals(exp);
      });
      if (first is not null) throw new ArgumentException($"All maps should have the same {field}[{exp}]. Found at lease one mismatch[{actual(first)}]");
    }
  }
  
  public async Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMapsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents) {
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
  
  public Task<List<Map.CoreToSysMap>> GetExistingMappingsFromCoreIds(SystemName system, CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (!coreids.Any()) return Task.FromResult<List<Map.CoreToSysMap>>([]);
    return GetExistingMapsByIds(system, coretype, coreids.Distinct().ToList());
  }

  public Task<List<Map.CoreToSysMap>> GetMapsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<SystemEntityId> systemids) {
    if (!systemids.Any()) return Task.FromResult<List<Map.CoreToSysMap>>([]);
    return GetExistingMapsByIds(system, coretype, systemids.Distinct().ToList());
  }
  
  public async Task<Dictionary<CoreEntityId, SystemEntityId>> GetRelatedSystemIdsFromCores(SystemName system, CoreEntityTypeName coretype, List<ICoreEntity> coreents, string foreignkey) {
    if (!coreents.Any()) return [];
    ValidateCoreEntitiesForQuery(coretype, coreents, false);
    
    var fks = coreents.Select(e => new CoreEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var maps = await GetExistingMappingsFromCoreIds(system, coretype, fks);
    var dict = maps.ToDictionary(m => m.CoreId, m => m.SystemId);
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntitySystemIdFromCoreId[{system}] - Could not find {coretype} with CoreIds [{String.Join(",", missing)}]");
    
    return dict;
  }
  public async Task<Dictionary<SystemEntityId, CoreEntityId>> GetRelatedCoreIdsFromSystemIds(SystemName system, CoreEntityTypeName coretype, List<ISystemEntity> sysents, string foreignkey, bool mandatory) {
    if (!sysents.Any()) return [];
    if (sysents.Any(e => SystemEntityTypeName.From(e) != SystemEntityTypeName.From(sysents.First()))) throw new ArgumentException($"All system entities should be of the same SystemEntityTypeName[{system}]");
    if (sysents.GroupBy(e => e.SystemId).Any(g => g.Count() > 1)) throw new ArgumentException("found duplicate system entities (by SystemId)");
    
    var fks = sysents.Select(e => new SystemEntityId(ReflectionUtils.GetPropValAsString(e, foreignkey))).Distinct().ToList();
    var dict = (await GetMapsFromSystemIds(system, coretype, fks)).ToDictionary(m => m.SystemId, m => m.CoreId);
    if (!mandatory) return dict;
    
    var missing = fks.Where(fk => !dict.ContainsKey(fk)).ToList();
    if (missing.Any()) throw new Exception($"FunctionHelpers.GetRelatedEntityCoreIdsFromSystemIds[{system}] - Could not find {coretype} with SystemIds [{String.Join(",", missing)}]");
    return dict;
  }
  
  private void ValidateCoreEntitiesForQuery(CoreEntityTypeName coretype, List<ICoreEntity> coreents, bool validatetype) {
    if (validatetype && coreents.Any(e => CoreEntityTypeName.From(e) != coretype)) throw new ArgumentException($"All core entities should match expected CoreEntityTypeName[{coretype}]");
    if (coreents.Any(e => CoreEntityTypeName.From(e) != CoreEntityTypeName.From(coreents.First()))) throw new ArgumentException($"All core entities should be of the same CoreEntityTypeName[{CoreEntityTypeName.From(coreents.First())}]");
    if (coreents.GroupBy(e => e.CoreId).Any(g => g.Count() > 1)) throw new ArgumentException("found duplicate core entities (by CoreId)");
  }
}
