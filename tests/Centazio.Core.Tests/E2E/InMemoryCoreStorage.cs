using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;
using Serilog;

namespace Centazio.Core.Tests.E2E;

public class InMemoryCoreStorage(SimulationCtx ctx) : AbstractCoreStorage(ctx.ChecksumAlg.Checksum) {
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public override Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.UpdatedCoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.UpdatedCoreEntity.CoreId)) { ctx.Epoch.Update(e.UpdatedCoreEntity); } 
      else { ctx.Epoch.Add(e.UpdatedCoreEntity); }
      
      target[e.UpdatedCoreEntity.CoreId] = Json.Serialize(e.UpdatedCoreEntity);
      return e.UpdatedCoreEntity;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }
  
  protected override Task<E> GetSingle<E, D>(CoreEntityId? coreid) {
    return Task.FromResult(GetSingleImpl<E, D>(coreid) ?? throw new Exception());
  }
  
  protected override Task<List<E>> GetList<E, D>() {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    var results = db[CoreEntityTypeName.From<E>()].Keys.Select(coreid => GetSingleImpl<E, D>(coreid) ?? throw new Exception()).ToList();
    return Task.FromResult(results);
  }

  
  
  protected E? GetSingleImpl<E, D>(CoreEntityId? coreid) where E : class, ICoreEntity where D : class, IDto<E> { 
    var dict = db[CoreEntityTypeName.From<E>()];
    if (coreid is null || !dict.TryGetValue(coreid, out var json)) return default;

    return (Json.Deserialize<D>(json) ?? throw new Exception()).ToBase();
  }

  public override ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }

}