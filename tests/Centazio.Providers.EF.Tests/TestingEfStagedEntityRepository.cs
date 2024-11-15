using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests;

public class TestingEfStagedEntityRepository(EFStagedEntityRepositoryOptions opts, IDbFieldsHelper dbf) : EFStagedEntityRepository(opts) {
  
  public override async Task<IStagedEntityRepository> Initialise() {
    await using var db = opts.Db();
    await DropTablesImpl(db);
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.StagedEntityTableName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], $"UNIQUE({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.StagedEntityChecksum)})"));
    var index = dbf.GenerateIndexScript(db.SchemaName, db.StagedEntityTableName, nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.DateStaged));
    await db.Database.ExecuteSqlRawAsync(index);
    
    return this;
  }

  public override async ValueTask DisposeAsync() {
    await using var db = opts.Db();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(AbstractStagedEntityRepositoryDbContext db) { await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.StagedEntityTableName)); }

}