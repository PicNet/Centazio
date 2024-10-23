using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

// todo: EFCore should handle `ValidStrings` like Dapper does
public class EFCoreStagedEntityRepository(int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityRepository(limit, checksum) {

  internal static readonly string STAGED_ENTITY_TBL = $"{nameof(Core.Ctl)}_{nameof(StagedEntity)}".ToLower();
  
  protected override async Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    await using var db = new StagedEntityContext();
    var newsums = staged.Select(s => s.StagedEntityChecksum.Value);
    var duplicates = Query(db, system, systype)
        .Where(s => newsums.Contains(s.StagedEntityChecksum ?? ""))
        .ToDictionary(s => s.StagedEntityChecksum!);
    
    var toinsert = staged.Where(s => !duplicates.ContainsKey(s.StagedEntityChecksum.Value)).ToList();
    db.Staged.AddRange(toinsert.Select(se => (StagedEntity.Dto) (DtoHelpers.ToDto(se) ?? throw new Exception())));
    await db.SaveChangesAsync();
    
    return toinsert;
  }
  
  public override async Task Update(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    if (!staged.Any()) return;
    await using var db = new StagedEntityContext();
    var ids = staged.Select(e => e.Id).ToList();
    var existing = Query(db, system, systype).Where(e => ids.Contains((Guid) e.Id!)).ToList();
    existing.ForEach(e => {
      var source = staged.Single(s => s.Id == e.Id);
      e.DatePromoted = source.DatePromoted;
      e.IgnoreReason = source.IgnoreReason;
    });
    await db.SaveChangesAsync();
  }

  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    await using var db = new StagedEntityContext();
    var query = Query(db, system, systype).Where(e => e.DateStaged > after && e.IgnoreReason == null);
    if (!incpromoted) query = query.Where(e => !e.DatePromoted.HasValue);
    if (Limit is > 0 and < Int32.MaxValue) query = query.Take(Limit);
    return query.ToList().Select(dto => dto.ToBase()).ToList();
  }

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    await using var db = new StagedEntityContext();
    var query = Query(db, system, systype); 
    query = promoted ? query.Where(e => e.DatePromoted < before) : query.Where(e => e.DateStaged < before);
    await query.ExecuteDeleteAsync();
  }

  private IQueryable<StagedEntity.Dto> Query(StagedEntityContext db, SystemName system, SystemEntityTypeName systype) => db.Staged.Where(e => e.System == system.Value && e.SystemEntityTypeName == systype.Value); 
  
  public async Task<EFCoreStagedEntityRepository> Initialise() {
    await using var db = new StagedEntityContext();
    var dbf = new DbFieldsHelper();
    await db.Database.ExecuteSqlRawAsync(dbf.GetSqliteCreateTableScript(STAGED_ENTITY_TBL, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], $"UNIQUE({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.StagedEntityChecksum)})"));
    await db.Database.ExecuteSqlRawAsync($"CREATE INDEX IF NOT EXISTS ix_{STAGED_ENTITY_TBL}_source_obj_staged ON [{STAGED_ENTITY_TBL}] ({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.DateStaged)});");
    return this;
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = new StagedEntityContext();
    await db.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {STAGED_ENTITY_TBL}");
  }
}

public class StagedEntityContext : DbContext {
  public DbSet<StagedEntity.Dto> Staged { get; set; }
  
  protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=staged_entity.db");

  protected override void OnModelCreating(ModelBuilder builder) {
    builder.Entity<StagedEntity.Dto>(e => e.ToTable(EFCoreStagedEntityRepository.STAGED_ENTITY_TBL));
  }

}