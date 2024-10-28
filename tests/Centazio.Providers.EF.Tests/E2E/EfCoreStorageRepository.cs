using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF.Tests.E2E;

public class EfCoreStorageRepository(Func<AbstractCoreStorageDbContext> getdb, IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum) : AbstractCoreStorageRepository(checksum) {
  
  public async Task<EfCoreStorageRepository> Initialise(IDbFieldsHelper dbf, bool reset = false) {
    await using var db = getdb();
    if (reset) await db.DropTables();
    await db.CreateTableIfNotExists(dbf);
    return this;
  }
  
  public override async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    // todo: clean this code
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.UpdatedCoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreId);
    await using var db = getdb();
    entities.ForEach(t => {
      var e = (CoreEntityBase) t.UpdatedCoreEntity;
      var dto = DtoHelpers.ToDto(e) ?? throw new Exception();
      db.Attach(dto);
      if (existing.ContainsKey(e.CoreId)) { tracker.Update(e); } 
      else { tracker.Add(e); }
      db.Entry(dto).State = existing.ContainsKey(e.CoreId) ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }
  
  protected override async Task<List<E>> GetList<E, D>() {
    await using var db = getdb();
    var lst = (await db.Set<D>().ToListAsync()).Select(dto => dto.ToBase()).ToList();
    return lst;
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