using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
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

  protected override Task<List<CoreEntityAndMeta>> GetList<E, D>(Expression<Func<CoreEntityAndMetaDtos<D>, bool>> predicate) {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    
    var results = db[coretype].Values
        .Select(Json.Deserialize<CoreEntityAndMetaDtos<D>>)
        .Where(predicate.Compile())
        .Select(dtos => new CoreEntityAndMeta(dtos.coreentdto.ToBase(), dtos.metadto.ToBase()))
        .ToList();
    return Task.FromResult(results);
  }
  
  public override ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}