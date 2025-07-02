using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Shared;

public class CoreStorageRepository(Func<CentazioDbContext> getdb,  IDbFieldsHelper dbf) : AbstractCoreStorageEfRepository(getdb) {
  
  public async Task<CoreStorageRepository> Initialise() {
    return await UseDb(async db => {
      await db.ExecSql(dbf.GenerateCreateTableScript("ctl", nameof(CoreStorageMeta).ToLower(), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
      await db.ExecSql(dbf.GenerateCreateTableScript("dbo", nameof(CoreTask).ToLower(), dbf.GetDbFields<CoreTask>(), [nameof(ICoreEntity.CoreId)]));

      return this;
    });
  }
  
  public async Task<List<CoreTask>> Tasks() => await UseDb(async db => await db.Set<CoreTask>().ToListAsync());

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    return await UseDb(async db => 
        (await db.Set<CoreTask>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).ToList<ICoreEntity>());
  }
}