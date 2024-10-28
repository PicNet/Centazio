using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public record EFStagedEntityRepositoryOptions(
    int Limit, 
    Func<string, StagedEntityChecksum> StagedEntityDataChecksum,
    Func<AbstractStagedEntityRepositoryDbContext> Db);

public class EFStagedEntityRepository(EFStagedEntityRepositoryOptions opts) : 
    AbstractStagedEntityRepository(opts.Limit, opts.StagedEntityDataChecksum) {
  
  protected readonly EFStagedEntityRepositoryOptions opts = opts;
  
  protected override async Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    await using var db = opts.Db();
    var newsums = staged.Select(s => s.StagedEntityChecksum.Value);
    var duplicates = Query(system, systype, db)
        .Where(s => newsums.Contains(s.StagedEntityChecksum ?? String.Empty))
        .ToDictionary(s => s.StagedEntityChecksum!);
    
    var toinsert = staged.Where(s => !duplicates.ContainsKey(s.StagedEntityChecksum.Value)).ToList();
    var dtos = toinsert.Select(DtoHelpers.ToDto<StagedEntity, StagedEntity.Dto>);
    db.Staged.AddRange(dtos);
    await db.SaveChangesAsync();
    
    return toinsert;
  }
  
  public override async Task UpdateImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    if (!staged.Any()) return;
    await using var db = opts.Db();
    await db.ToDtoAttachAndUpdate<StagedEntity, StagedEntity.Dto>(staged);
  }

  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    await using var db = opts.Db();
    var query = Query(system, systype, db).Where(e => e.DateStaged > after && String.IsNullOrEmpty(e.IgnoreReason));
    if (!incpromoted) query = query.Where(e => !e.DatePromoted.HasValue);
    if (Limit is > 0 and < Int32.MaxValue) query = query.Take(Limit);
    return query.OrderBy(e => e.DateStaged).ToList().Select(dto => dto.ToBase()).ToList();
  }

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    await using var db = opts.Db();
    var query = Query(system, systype, db); 
    query = promoted ? query.Where(e => e.DatePromoted < before) : query.Where(e => e.DateStaged < before);
    await query.ExecuteDeleteAsync();
  }

  private IQueryable<StagedEntity.Dto> Query(SystemName system, SystemEntityTypeName systype, AbstractStagedEntityRepositoryDbContext db) => 
      db.Staged.Where(e => e.System == system.Value && e.SystemEntityTypeName == systype.Value); 
  
  public override Task<AbstractStagedEntityRepository> Initialise() => Task.FromResult<AbstractStagedEntityRepository>(this);
  public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

}