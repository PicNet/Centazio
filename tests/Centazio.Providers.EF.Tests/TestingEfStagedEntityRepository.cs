namespace Centazio.Providers.EF.Tests;

public class TestingEfStagedEntityRepository(EFStagedEntityRepositoryOptions opts) : EFStagedEntityRepository(opts, true) {
  
  public override async ValueTask DisposeAsync() {
    await using var db = opts.Db();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(AbstractStagedEntityRepositoryDbContext db) => 
      await db.DropDb([new (db.SchemaName, db.StagedEntityTableName)]);

}