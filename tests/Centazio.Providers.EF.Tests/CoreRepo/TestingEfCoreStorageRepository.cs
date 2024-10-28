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
    
    var fields = dbf.GetDbFields<CoreEntity>();
    fields.Add(new (nameof(CoreEntityChecksum), typeof(string), ChecksumValue.MAX_LENGTH.ToString(), true));
    await conn.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(conn.SchemaName, nameof(CoreEntity), fields, [nameof(CoreEntity.CoreId)]));
    return this;
  }
  
  public async Task<List<ICoreEntity>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var conn = getdb();
    return conn.CoreEntities
        .Where(e => e.System != exclude.Value && e.DateUpdated > after)
        .ToList()
        .Select(dto => (ICoreEntity) dto.ToBase()).ToList();
  }

  public async Task<List<ICoreEntity>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype != CoreEntityTypeName.From<CoreEntity>()) throw new Exception();
    await using var conn = getdb();
    var idstrs = coreids.Select(id => id.Value);
    var entities = conn.CoreEntities
        .Where(e => idstrs.Contains(e.CoreId))
        .ToList()
        .Select(dto => (ICoreEntity) dto.ToBase()).ToList();
    if (entities.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return entities;
  }
  
  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    await using var conn = getdb();
    return (await conn.CoreEntities.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = getdb();
    var idstrs = coreids.Select(id => id.Value);
    return await conn.CoreEntities.Where(e => idstrs.Contains(e.CoreId)).ToDictionaryAsync(dto => new CoreEntityId(dto.CoreId ?? throw new Exception()), dto => new CoreEntityChecksum(dto.CoreEntityChecksum ?? throw new Exception()));
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    await using var conn = getdb();
    var ids = entities.Select(e => e.UpdatedCoreEntity.CoreId.Value);
    var existings = conn.CoreEntities.Where(e => ids.Contains(e.CoreId)).Select(e => e.CoreId);
    entities.ForEach(e => {
      // todo: why does ToDto return nullable
      var dto = (CoreEntity.Dto) DtoHelpers.ToDto(e.UpdatedCoreEntity)! with { CoreEntityChecksum = e.UpdatedCoreEntityChecksum };
      conn.Attach(dto);
      conn.Entry(dto).State = existings.Contains(dto.CoreId) ? EntityState.Modified : EntityState.Added;
    });
    await conn.SaveChangesAsync();
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }

  public async ValueTask DisposeAsync() {
    await using var conn = getdb();
    await conn.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(conn.SchemaName, conn.CoreEntityName));
  }
}