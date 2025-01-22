using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Types;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF;

public abstract class AbstractCoreStorageEfRepository(Func<CentazioDbContext> getdb, Func<ICoreEntity, CoreEntityChecksum> checksum) : ICoreStorage {
  protected Func<CentazioDbContext> Db => getdb;
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    await using var db = Db();
    var metas = (await db.Set<CoreStorageMeta.Dto>()
            .Where(m => m.CoreEntityTypeName == coretype && m.LastUpdateSystem != exclude.Value && m.DateUpdated > after)
            .ToListAsync())
        .Select(m => m.ToBase())
        .ToList(); 
    return await GetCoresForMetas(coretype, metas, db);
  }
  
  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var db = Db();
    var cids = coreids.Select(cid => cid.Value).ToList();
    var metas = (await db.Set<CoreStorageMeta.Dto>()
            .Where(m => m.CoreEntityTypeName == coretype && cids.Contains(m.CoreId))
            .ToListAsync())
        .Select(m => m.ToBase())
        .ToList();
    return await GetCoresForMetas(coretype, metas, db);
  }
  
  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var cems = await  GetExistingEntities(coretype, coreids);
    return cems.ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));
  }
  
  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.CoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreEntity.CoreId);
    await using var db = Db();
    
    entities.ForEach(t => {
      var state = existing.ContainsKey(t.CoreEntity.CoreId) ? EntityState.Modified : EntityState.Added;
      var dtos = t.ToDtos();
      db.Attach(dtos.CoreEntityDto).State = state;
      db.Attach(dtos.MetaDto).State = state;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities;
  }
  
  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  
  private async Task<List<CoreEntityAndMeta>> GetCoresForMetas(CoreEntityTypeName coretype, List<CoreStorageMeta> metas, CentazioDbContext db) {
    var tasks = await GetCoreEntitiesWithIds(coretype, metas.Select(m => m.CoreId).ToList(), db);
    return metas.Select(meta => {
      var task = tasks.Single(t => t.CoreId == meta.CoreId);
      return new CoreEntityAndMeta(task, meta);
    }).ToList();
  }
  

  protected abstract Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids, CentazioDbContext db);

}