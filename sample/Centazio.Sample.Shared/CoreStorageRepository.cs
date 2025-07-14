using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Shared;

public class CoreStorageRepository(Func<CentazioDbContext> getdb) : AbstractCoreStorageEfRepository(getdb) {
  
  public async Task<CoreStorageRepository> Initialise() {
    return await UseDb(async db => {
      await db.DropDb(new ("ctl", nameof(CoreStorageMeta).ToLower()), new ("dbo", nameof(CoreTask).ToLower()));
      await db.CreateDb();

      return this;
    });
  }
  
  public async Task<List<CoreTask.Dto>> Tasks() => await UseDb(async db => await db.Set<CoreTask.Dto>().ToListAsync());

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    return await UseDb(async db => 
        (await db.Set<CoreTask.Dto>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList());
  }
}