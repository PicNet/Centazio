using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public class TestingEfCoreStorageRepository(Func<AbstractTestingCoreStorageDbContext> getdb, IDbFieldsHelper dbf) : ITestingCoreStorage {

  public async Task<ITestingCoreStorage> Initalise() {
    await using var db = getdb();
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.CtlSchemaName, db.CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.CoreSchemaName, db.CoreEntityName, dbf.GetDbFields<CoreEntity>(), [nameof(CoreEntity.CoreId)]));
    return this;
  }
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var db = getdb();
    var metas = await db.Metas.Where(m => m.LastUpdateSystem != exclude.Value && m.CoreEntityTypeName == coretype.Value && m.DateUpdated > after).ToListAsync();
    var cids = metas.Select(m => m.CoreId);
    var cores = await db.CoreEntities.Where(e => cids.Contains(e.CoreId)).ToListAsync();
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var db = getdb();
    var idstrs = coreids.Select(id => id.Value);
    var metas = await db.Metas.Where(m => m.CoreEntityTypeName == coretype.Value && idstrs.Contains(m.CoreId)).ToListAsync();
    var cores = await db.CoreEntities.Where(e => idstrs.Contains(e.CoreId)).ToListAsync();
    if (cores.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }
  
  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    await using var db = getdb();
    return (await db.CoreEntities.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var db = getdb();
    var idstrs = coreids.Select(id => id.Value);
    return await db.Metas
        .Where(m => idstrs.Contains(m.CoreId))
        .ToDictionaryAsync(
            dto => new CoreEntityId(dto.CoreId ?? throw new Exception()), 
            dto => new CoreEntityChecksum(dto.CoreEntityChecksum ?? throw new Exception()));
  }
  

  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    await using var db = getdb();
    var ids = entities.Select(e => e.CoreEntity.CoreId.Value);
    var existings = db.CoreEntities.Where(e => ids.Contains(e.CoreId)).Select(e => e.CoreId).ToList();
    entities.ForEach(e => {
      var dtos = e.ToDtos();
      var isupdate = existings.Contains(e.CoreEntity.CoreId.Value);
      db.Attach(dtos.CoreEntityDto).State = isupdate ? EntityState.Modified : EntityState.Added;
      db.Attach(dtos.MetaDto).State = isupdate ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();
    return entities;
  }

  public async ValueTask DisposeAsync() {
    await using var db = getdb();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.CoreSchemaName, db.CoreEntityName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.CtlSchemaName, db.CoreStorageMetaName));
  }
}