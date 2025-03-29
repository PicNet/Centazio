using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace {{ it.Namespace }};

public class CoreStorageRepository(Func<CentazioDbContext> getdb,  IDbFieldsHelper dbf) : AbstractCoreStorageEfRepository(getdb) {
  
  public async Task<CoreStorageRepository> Initialise() {
    await using var db = Db();
    
    await db.ExecSql(dbf.GenerateCreateTableScript("ctl", nameof(CoreStorageMeta).ToLower(), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
    await db.ExecSql(dbf.GenerateCreateTableScript("dbo", nameof(ExampleEntity).ToLower(), dbf.GetDbFields<ExampleEntity>(), [nameof(ICoreEntity.CoreId)]));
    
    return this;
  }
  
  public DbSet<ExampleEntity.Dto> Tasks(CentazioDbContext db) => db.Set<ExampleEntity.Dto>();

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids, CentazioDbContext db) {
    var strids = coreids.Select(id => id.Value).ToList();
    return (await db.Set<ExampleEntity.Dto>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList();
  }
}