using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample;

public class SampleCoreStorageRepository(Func<CentazioDbContext> getdb,  IDbFieldsHelper dbf, Func<ICoreEntity, CoreEntityChecksum> checksum) : AbstractCoreStorageEfRepository(getdb, checksum) {
  
  public async Task Initialise() {
    await using var db = Db();
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(nameof(Core.Ctl).ToLower(), nameof(CoreStorageMeta).ToLower(), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript("dbo", nameof(CoreTask).ToLower(), dbf.GetDbFields<CoreTask>(), [nameof(ICoreEntity.CoreId)]));
  }

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> ids, CentazioDbContext db) {
    var strids = ids.Select(id => id.Value).ToList();
    return (await db.Set<CoreTask.Dto>().Where(t => strids.Contains(t.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList();
  }
}