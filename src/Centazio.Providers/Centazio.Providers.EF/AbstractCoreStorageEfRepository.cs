using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF;

public abstract class AbstractCoreStorageEfRepository(Func<CentazioDbContext> getdb) : ICoreStorage {
  
  private readonly EfTransactionManager<CentazioDbContext> mgr = new(getdb);
  
  public Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) => mgr.BeginTransaction(reuse);
  protected Task<T> UseDb<T>(Func<CentazioDbContext, Task<T>> func) => mgr.UseDb(func);

  public virtual ValueTask DisposeAsync() => mgr.DisposeAsync();
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) => 
      await UseDb(async db => {
        var metas = (await db.Set<CoreStorageMeta.Dto>()
                .Where(m => m.CoreEntityTypeName == coretype && m.LastUpdateSystem != exclude.Value && m.DateUpdated > after)
                .ToListAsync())
            .Select(m => m.ToBase())
            .ToList(); 
        return await GetCoresForMetas(coretype, metas, db);
      });

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      await UseDb(async db => {
        var strids = coreids.Select(cid => cid.Value).ToList();
        var metas = (await db.Set<CoreStorageMeta.Dto>()
                .Where(m => m.CoreEntityTypeName == coretype.Value && strids.Contains(m.CoreId))
                .ToListAsync())
            .Select(m => m.ToBase())
            .ToList();
        if (coreids.Count != metas.Count) throw new Exception("Could not find all specified core entities");
        return await GetCoresForMetas(coretype, metas, db);
      });

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      await UseDb(async db => {
        var strids = coreids.Select(cid => cid.Value).ToList();
        return await db.Set<CoreStorageMeta.Dto>()
            .Where(m => m.CoreEntityTypeName == coretype.Value && strids.Contains(m.CoreId))
            .ToDictionaryAsync(m => new CoreEntityId(m.CoreId), m => new CoreEntityChecksum(m.CoreEntityChecksum ?? throw new Exception()));
      });

  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) => 
      await UseDb(async db => {
        var strids = entities.Select(e => e.CoreEntity.CoreId.Value).ToList();
        var existing = await db.Set<CoreStorageMeta.Dto>()
            .Where(m => m.CoreEntityTypeName == coretype.Value && strids.Contains(m.CoreId)).ToDictionaryAsync(m => m.CoreId);
        entities.ForEach(entity => UpsertEntity(entity, existing.ContainsKey(entity.CoreEntity.CoreId) ? EntityState.Modified : EntityState.Added, db));
        await db.SaveChangesAsync();

        Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.GetShortDisplayName()}")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
        return entities;
      });

  protected virtual void UpsertEntity(CoreEntityAndMeta entity, EntityState state, CentazioDbContext db) {
    var dtos = entity.ToDtos();
    db.Attach(dtos.CoreEntityDto).State = state;
    db.Attach(dtos.MetaDto).State = state;
  }
  
  // todo: can we replace these args `CentazioDbContext db` with a transaction which would achieve the same?
  private async Task<List<CoreEntityAndMeta>> GetCoresForMetas(CoreEntityTypeName coretype, List<CoreStorageMeta> metas, CentazioDbContext db) {
    var tasks = await GetCoreEntitiesWithIds(coretype, metas.Select(m => m.CoreId).ToList(), db);
    return metas.Select(meta => {
      var task = tasks.Single(t => t.CoreId == meta.CoreId);
      return new CoreEntityAndMeta(task, meta);
    }).ToList();
  }
  
  // todo: can we replace these args `CentazioDbContext db` with a transaction which would achieve the same?
  protected abstract Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids, CentazioDbContext db);

}