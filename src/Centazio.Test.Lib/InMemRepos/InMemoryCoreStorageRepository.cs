using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;
using Serilog;

namespace Centazio.Test.Lib.InMemRepos;

public class InMemoryCoreStorageRepository(IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum) : AbstractCoreStorageRepository(checksum) {
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public override Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<(CoreEntityAndMeta UpdatedCoreEntityAndMeta, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId)) { tracker.Update(e.UpdatedCoreEntityAndMeta); } 
      else { tracker.Add(e.UpdatedCoreEntityAndMeta); }
      
      target[e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId] = Json.Serialize(e.UpdatedCoreEntityAndMeta.ToDtos());
      return e.UpdatedCoreEntityAndMeta;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntityAndMeta.CoreEntity.DisplayName}({e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }
  

  protected override Task<E> GetSingle<E, D>(CoreEntityId coreid) {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (!dict.TryGetValue(coreid, out var json)) throw new Exception();
    return Task.FromResult(CoreEntityAndMeta.FromJson<E, D>(json).As<E>());
  }

  protected override Task<List<CoreEntityAndMeta>> GetList<E, D>(Expression<Func<D, bool>> predicate) {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    
    var where = predicate.Compile();
    var results = db[coretype].Values
        .Select(CoreEntityAndMeta.FromJson<E, D>)
        .Where(ceam => where(DtoHelpers.ToDto<E, D>((E) ceam.CoreEntity)))
        .ToList();
    return Task.FromResult(results);
  }
  
  public override ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}