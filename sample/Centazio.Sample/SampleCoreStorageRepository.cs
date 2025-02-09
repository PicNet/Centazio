using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample;

public class SampleCoreStorageRepository(Func<CentazioDbContext> getdb,  IDbFieldsHelper dbf) : AbstractCoreStorageEfRepository(getdb) {
  
  public async Task<SampleCoreStorageRepository> Initialise() {
    await using var db = Db();
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(nameof(Core.Ctl).ToLower(), nameof(CoreStorageMeta).ToLower(), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript("dbo", nameof(CoreTask).ToLower(), dbf.GetDbFields<CoreTask>(), [nameof(ICoreEntity.CoreId)]));
    
    return this;
  }
  
  public DbSet<CoreTask.Dto> Tasks(CentazioDbContext db) => db.Set<CoreTask.Dto>();

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids, CentazioDbContext db) {
    var strids = coreids.Select(id => id.Value).ToList();
    return (await db.Set<CoreTask.Dto>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList();
  }
}