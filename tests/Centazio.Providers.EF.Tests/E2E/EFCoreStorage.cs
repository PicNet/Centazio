using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class EFCoreStorage(SimulationCtx ctx, Func<AbstractCoreStorageDbContext> getdb) : AbstractCoreStorage(ctx.ChecksumAlg.Checksum) {
  
  public async Task<EFCoreStorage> Initialise() {
    await using var db = getdb();
    await db.CreateTableIfNotExists();
    return this;
  }
  
  public override async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    // todo: clean this code
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.UpdatedCoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreId);
    await using var db = getdb();
    entities.ForEach(t => {
      var e = (CoreEntityBase) t.UpdatedCoreEntity;
      if (e.CoreEntityChecksum != t.UpdatedCoreEntityChecksum) throw new Exception($"is it set old[{e.CoreEntityChecksum}] new[{t.UpdatedCoreEntityChecksum}]");
      e.CoreEntityChecksum = t.UpdatedCoreEntityChecksum;
      var dto = DtoHelpers.ToDto(e) ?? throw new Exception();
      db.Attach(dto);
      if (existing.ContainsKey(e.CoreId)) {
        ctx.Epoch.Update(e); 
      } 
      else { ctx.Epoch.Add(e); }
      db.Entry(dto).State = existing.ContainsKey(e.CoreId) ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }
  
  protected override async Task<List<E>> GetList<E, D>() {
    await using var db = getdb();
    return (await db.Set<D>().ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
  
  // todo: why is coreid nullable
  protected override async Task<E> GetSingle<E, D>(CoreEntityId? coreid) where E : class {
    if (coreid is null) throw new Exception("todo: why is coreid nullable");
    await using var db = getdb();
    return (await db.Set<D>().SingleAsync(dto => dto.CoreId == coreid.Value)).ToBase();
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = getdb();
    await db.DropTables();
  }

}