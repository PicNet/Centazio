using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib.E2E;
using Serilog;

namespace Centazio.Test.Lib.InMemRepos;

public class InMemoryCoreStorageRepository(IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum) : AbstractCoreStorageRepository(checksum) {
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public override Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.CoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.CoreEntity.CoreId)) { tracker.Update(e); } 
      else { tracker.Add(e); }
      
      target[e.CoreEntity.CoreId] = Json.Serialize(e.ToDtos());
      return e;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }
  

  protected override Task<E> GetSingle<E, D>(CoreEntityId coreid) {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (!dict.TryGetValue(coreid, out var json)) throw new Exception();
    return Task.FromResult(CoreEntityAndMeta.FromJson<E, D>(json).As<E>());
  }

  protected override async Task<List<CoreEntityAndMeta>> GetExistingEntities<E, D>(List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    return (await ListImpl<E, D>())
        .Where(d => strids.Contains(d.MetaDto.CoreId))
        .Select(d => new CoreEntityAndMeta(d.CoreEntityDto.ToBase(), d.MetaDto.ToBase()))
        .ToList();
  }

  protected override async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E, D>(SystemName exclude, DateTime after) {
    return (await ListImpl<E, D>())
        .Where(d => d.MetaDto.DateUpdated > after && d.MetaDto.LastUpdateSystem != exclude.Value)
        .Select(d => new CoreEntityAndMeta(d.CoreEntityDto.ToBase(), d.MetaDto.ToBase()))
        .ToList();
  }

  private Task<List<CoreEntityAndMetaDtos<D>>> ListImpl<E, D>() where E : ICoreEntity {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    return Task.FromResult(db[coretype].Values.Select(Json.Deserialize<CoreEntityAndMetaDtos<D>>).ToList());
  }
  
  public override ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}