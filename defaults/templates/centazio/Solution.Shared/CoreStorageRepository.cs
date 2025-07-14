using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace {{ it.Namespace }};

public class CoreStorageRepository(Func<CentazioDbContext> getdb) : AbstractCoreStorageEfRepository(getdb) {
  
  public async Task<CoreStorageRepository> Initialise() {
    return await UseDb(async db => {
      await db.CreateDb();
      return this;
    });
  }
  
  public async Task<List<ExampleEntity.Dto>> Tasks() => await UseDb(async db => await db.Set<ExampleEntity.Dto>().ToListAsync());

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    return await UseDb(async db =>
      (await db.Set<ExampleEntity.Dto>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList());
  }
}