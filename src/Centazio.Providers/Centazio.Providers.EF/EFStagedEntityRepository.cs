﻿using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
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
  protected override async Task<List<StagedEntityChecksum>> GetDuplicateChecksums(SystemName system, SystemEntityTypeName systype, List<StagedEntityChecksum> newchecksums) {
    await using var db = opts.Db();
    var checksumstrs = newchecksums.Select(cs => cs.Value).ToList();
    return await Query(system, systype, db)
        .Where(s => checksumstrs.Contains(s.StagedEntityChecksum ?? String.Empty))
        .Select(s => new StagedEntityChecksum(s.StagedEntityChecksum ?? String.Empty))
        .ToListAsync();
  }

  protected override async Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    await using var db = opts.Db();
    var dtos = staged.Select(DtoHelpers.ToDto<StagedEntity, StagedEntity.Dto>);
    db.Staged.AddRange(dtos);
    await db.SaveChangesAsync();
    
    return staged;
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
  
  public override Task<IStagedEntityRepository> Initialise() => Task.FromResult<IStagedEntityRepository>(this);
  public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

}