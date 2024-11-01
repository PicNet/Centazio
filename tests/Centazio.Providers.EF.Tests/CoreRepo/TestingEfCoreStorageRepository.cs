using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public class TestingEfCoreStorageRepository(Func<AbstractTestingCoreStorageDbContext> getdb, IDbFieldsHelper dbf) : ITestingCoreStorage {

  public async Task<ITestingCoreStorage> Initalise() {
    await using var conn = getdb();
    
    await conn.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(conn.SchemaName, nameof(CoreEntity), dbf.GetDbFields<CoreEntity>(), [nameof(CoreEntity.CoreId)]));
    await conn.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(conn.SchemaName, nameof(CoreStorageMeta), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreId)]));
    return this;
  }
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    // todo: rename conn to db everywhere
    await using var conn = getdb();
    var metas = await conn.Metas.Where(m => m.OriginalSystem != exclude.Value && m.CoreEntityTypeName == coretype.Value && m.DateUpdated > after).ToListAsync();
    var cids = metas.Select(m => m.CoreId);
    var cores = await conn.CoreEntities.Where(e => cids.Contains(e.CoreId)).ToListAsync();
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var conn = getdb();
    var idstrs = coreids.Select(id => id.Value);
    var metas = await conn.Metas.Where(m => m.CoreEntityTypeName == coretype.Value && idstrs.Contains(m.CoreId)).ToListAsync();
    var cores = await conn.CoreEntities.Where(e => idstrs.Contains(e.CoreId)).ToListAsync();
    if (cores.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }
  
  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    await using var conn = getdb();
    return (await conn.CoreEntities.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = getdb();
    var idstrs = coreids.Select(id => id.Value);
    return await conn.Metas
        .Where(m => idstrs.Contains(m.CoreId))
        .ToDictionaryAsync(
            dto => new CoreEntityId(dto.CoreId ?? throw new Exception()), 
            dto => new CoreEntityChecksum(dto.CoreEntityChecksum ?? throw new Exception()));
  }
  

  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    await using var conn = getdb();
    var ids = entities.Select(e => e.CoreEntity.CoreId.Value);
    var existings = conn.CoreEntities.Where(e => ids.Contains(e.CoreId)).Select(e => e.CoreId).ToList();
    entities.ForEach(e => {
      var dtos = e.ToDtos();
      var isupdate = existings.Contains(e.CoreEntity.CoreId.Value);
      conn.Attach(dtos.CoreEntityDto).State = isupdate ? EntityState.Modified : EntityState.Added;
      conn.Attach(dtos.MetaDto).State = isupdate ? EntityState.Modified : EntityState.Added;
    });
    await conn.SaveChangesAsync();
    return entities;
  }

  public async ValueTask DisposeAsync() {
    await using var conn = getdb();
    await conn.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(conn.SchemaName, conn.CoreEntityName));
  }
}