﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public class InMemoryEntityIntraSystemMappingStore : AbstractEntityIntraSystemMappingStore {

  protected readonly Dictionary<EntityIntraSysMap.MappingKey, EntityIntraSysMap> memdb = new();
  public override Task<List<EntityIntraSysMap>> GetAll() => Task.FromResult(memdb.Values.ToList());
  public override Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key) => Task.FromResult(memdb[key]);

  public override Task<GetForCoresResult> GetForCores(List<ICoreEntity> cores, SystemName target, CoreEntityType obj) {
    var news = new List<CoreAndPendingCreateMap>();
    var updates = new List<CoreAndPendingUpdateMap>();
    cores.ForEach(c => {
      var existing = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.CoreId == c.Id && k.SourceSystem == c.SourceSystem && k.SourceId == c.SourceId && k.TargetSystem == target);
      if (existing == default) news.Add(new CoreAndPendingCreateMap(c, EntityIntraSysMap.Create(c, target, obj)));
      else updates.Add(new CoreAndPendingUpdateMap(c, memdb[existing].Update()));
    });
    return Task.FromResult(new GetForCoresResult(news, updates));
  }
  
  public override Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName target, List<string> coreids) {
    return Task.FromResult(coreids.Select(cid => {
      var key = memdb.Keys.SingleOrDefault(k => k.CoreEntity == coretype && k.CoreId == cid && k.TargetSystem == target);
      return key is null ? null : memdb[key];
    })
        .Where(m => m is not null)
        .Cast<EntityIntraSysMap>()
        .ToList());
  }
  
  public override Task<string?> GetCoreIdForTargetSys(CoreEntityType obj, string targetid, SystemName targetsys) {
    // try to get the core from target system first (assumes `system` is the TargetSystem)
    var coreid = memdb.Keys.SingleOrDefault(k => k.CoreEntity == obj && k.TargetSystem == targetsys && k.TargetId == targetid)?.CoreId.Value;
    return Task.FromResult(coreid);
  }

  public override Task<List<EntityIntraSysMap.Created>> Create(List<EntityIntraSysMap.Created> news) => 
      Task.FromResult(news.Select(map => (EntityIntraSysMap.Created)(memdb[map.Key] = map)).ToList());

  public override Task<List<EntityIntraSysMap.Updated>> Update(List<EntityIntraSysMap.Updated> updates) {
    return Task.FromResult(updates.Select(map => (EntityIntraSysMap.Updated)(memdb[map.Key] = map)).ToList());
  }

  public override Task<List<string>> FilterOutBouncedBackIds(SystemName promotingsys, CoreEntityType obj, List<string> ids) {
    var bounces = memdb.Values.
      Where(map => map.CoreEntity == obj 
          && map.TargetSystem == promotingsys 
          && ids.Contains(map.TargetId)).
      Select(map => map.TargetId.Value).
      ToList();
    return Task.FromResult(ids.Except(bounces).ToList());
  }

  public override ValueTask DisposeAsync() { 
    memdb.Clear();
    return ValueTask.CompletedTask;
  }
}