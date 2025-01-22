using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public class TestingEfCoreStorageRepository(Func<CentazioDbContext> getdb, IDbFieldsHelper dbf) : ITestingCoreStorage {
  
  private static string CoreSchemaName => "dbo";
  private static string CtlSchemaName => nameof(Core.Ctl);
  private static string CoreEntityName => nameof(CoreEntity).ToLower();
  private static string CoreStorageMetaName => nameof(CoreStorageMeta).ToLower();
  
  private DbSet<CoreEntity.Dto> CoreEntities(CentazioDbContext db) => db.Set<CoreEntity.Dto>();
  private DbSet<CoreStorageMeta.Dto> Metas(CentazioDbContext db) => db.Set<CoreStorageMeta.Dto>();

  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var db = getdb();
    var metas = await Metas(db).Where(m => m.LastUpdateSystem != exclude.Value && m.CoreEntityTypeName == coretype.Value && m.DateUpdated > after).ToListAsync();
    var cids = metas.Select(m => m.CoreId);
    var cores = await CoreEntities(db).Where(e => cids.Contains(e.CoreId)).ToListAsync();
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var db = getdb();
    var idstrs = coreids.Select(id => id.Value);
    var metas = await Metas(db).Where(m => m.CoreEntityTypeName == coretype.Value && idstrs.Contains(m.CoreId)).ToListAsync();
    var cores = await CoreEntities(db).Where(e => idstrs.Contains(e.CoreId)).ToListAsync();
    if (cores.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return metas.Select(m => {
      var core = cores.Single(e => e.CoreId == m.CoreId); 
      return new CoreEntityAndMeta(core.ToBase(), m.ToBase());
    }).ToList();
  }
  
  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    await using var db = getdb();
    return (await CoreEntities(db).ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var db = getdb();
    var idstrs = coreids.Select(id => id.Value);
    return await Metas(db)
        .Where(m => idstrs.Contains(m.CoreId))
        .ToDictionaryAsync(
            dto => new CoreEntityId(dto.CoreId ?? throw new Exception()), 
            dto => new CoreEntityChecksum(dto.CoreEntityChecksum ?? throw new Exception()));
  }
  

  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    await using var db = getdb();
    var ids = entities.Select(e => e.CoreEntity.CoreId.Value);
    var existings = CoreEntities(db).Where(e => ids.Contains(e.CoreId)).Select(e => e.CoreId).ToList();
    entities.ForEach(e => {
      var dtos = e.ToDtos();
      var isupdate = existings.Contains(e.CoreEntity.CoreId.Value);
      db.Attach(dtos.CoreEntityDto).State = isupdate ? EntityState.Modified : EntityState.Added;
      db.Attach(dtos.MetaDto).State = isupdate ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();
    return entities;
  }

  public async Task<ITestingCoreStorage> Initalise() {
    await using var db = getdb();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CtlSchemaName, CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CoreSchemaName, CoreEntityName, dbf.GetDbFields<CoreEntity>(), [nameof(CoreEntity.CoreId)]));
    return this;
  }
  
  public async ValueTask DisposeAsync() {
    await using var db = getdb();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CoreSchemaName, CoreEntityName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CtlSchemaName, CoreStorageMetaName));
  }
  
  public static void CreateTestingCoreStorageEfModel(ModelBuilder builder) => builder
      .HasDefaultSchema(CoreSchemaName)
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName, CtlSchemaName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreEntity.Dto>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      });
}