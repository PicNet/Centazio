using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF;

public abstract class AbstractCoreStorageEfRepository(Func<CentazioDbContext> getdb) : ICoreStorage {
  
  private readonly EfTransactionManager<CentazioDbContext> mgr = new(getdb);
  
  protected event EventHandler<EntityUpsertEventArgs>? OnEntityAdd;
  protected event EventHandler<EntityUpsertEventArgs>? OnEntityUpdate;
  
  public Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) => mgr.BeginTransaction(reuse);
  
  protected Task<T> UseDb<T>(DbOperation<CentazioDbContext, T> func) => mgr.UseDb(func);

  public virtual ValueTask DisposeAsync() => mgr.DisposeAsync();
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) => 
      await UseDb(async db => {
        var metas = (await db.Set<CoreStorageMeta.Dto>()
                .Where(m => m.CoreEntityTypeName == coretype && m.LastUpdateSystem != exclude.Value && m.DateUpdated > after)
                .ToListAsync())
            .Select(m => m.ToBase())
            .ToList(); 
        return await GetCoresForMetas(coretype, metas);
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
        return await GetCoresForMetas(coretype, metas);
      });

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      await UseDb(async db => {
        var strids = coreids.Select(cid => cid.Value).ToList();
        return await db.Set<CoreStorageMeta.Dto>()
            .Where(m => m.CoreEntityTypeName == coretype.Value && strids.Contains(m.CoreId))
            .ToDictionaryAsync(m => new CoreEntityId(m.CoreId), m => new CoreEntityChecksum(m.CoreEntityChecksum ?? throw new Exception()));
      });

  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    return await UseDb(async db => {
      var strids = entities.Select(e => e.CoreEntity.CoreId.Value).ToList();
      var existing = await db.Set<CoreStorageMeta.Dto>()
          .Where(m => m.CoreEntityTypeName == coretype.Value && strids.Contains(m.CoreId))
          .ToDictionaryAsync(m => m.CoreId);
      entities.ForEach(entity => UpsertEntity(entity, existing.ContainsKey(entity.CoreEntity.CoreId) ? EntityState.Modified : EntityState.Added, db));
      var dbresult = await db.SaveChangesAsync();
      Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.GetShortDisplayName()}")) +
          $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}] DbResult[{dbresult}]");
      return entities;
    });
    
    void UpsertEntity(CoreEntityAndMeta entity, EntityState state, CentazioDbContext db) {
      var dtos = entity.ToDtos();
      db.Attach(dtos.CoreEntityDto).State = state;
      db.Attach(dtos.MetaDto).State = state;
      
      if (state == EntityState.Added) OnEntityAdd?.Invoke(this, new EntityUpsertEventArgs(entity));
      else OnEntityUpdate?.Invoke(this, new EntityUpsertEventArgs(entity));
    }
  }

  private async Task<List<CoreEntityAndMeta>> GetCoresForMetas(CoreEntityTypeName coretype, List<CoreStorageMeta> metas) {
    var tasks = await GetCoreEntitiesWithIds(coretype, metas.Select(m => m.CoreId).ToList());
    return metas.Select(meta => {
      var task = tasks.Single(t => t.CoreId == meta.CoreId);
      return new CoreEntityAndMeta(task, meta);
    }).ToList();
  }
  
  protected abstract Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids);

  public class EntityUpsertEventArgs(CoreEntityAndMeta entity) : EventArgs {
    public CoreEntityAndMeta Entity { get; private set; } = entity;
  }
}