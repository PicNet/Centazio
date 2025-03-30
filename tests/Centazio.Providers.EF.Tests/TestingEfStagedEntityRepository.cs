using Centazio.Core.Misc;
using Centazio.Core.Stage;

namespace Centazio.Providers.EF.Tests;

public class TestingEfStagedEntityRepository(EFStagedEntityRepositoryOptions opts, IDbFieldsHelper dbf) : EFStagedEntityRepository(opts) {
  
  public override async Task<IStagedEntityRepository> Initialise() {
    await using var db = opts.Db();
    await DropTablesImpl(db);
    
    var createsql = dbf.GenerateCreateTableScript(db.SchemaName, db.StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)],
        [[nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.StagedEntityChecksum)]]);
    await db.ExecSql(createsql);
    var ixsql = dbf.GenerateIndexScript(db.SchemaName, db.StagedEntityTableName, nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.DateStaged));
    await db.ExecSql(ixsql);
    
    return this;
  }

  public override async ValueTask DisposeAsync() {
    await using var db = opts.Db();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(AbstractStagedEntityRepositoryDbContext db) => 
      await db.ExecSql(dbf.GenerateDropTableScript(db.SchemaName, db.StagedEntityTableName));

}