using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EFStagedEntityRepositoryOptions(
        settings.Limit, 
        new Sha256ChecksumAlgorithm().Checksum, 
        () => new SqliteStagedEntityContext(settings.ConnectionString));
    return new SqliteStagedEntityRepository(opts, new SqliteDbFieldsHelper(), settings.CreateSchema);
  }
}

public class SqliteStagedEntityRepository(EFStagedEntityRepositoryOptions opts, IDbFieldsHelper dbf, bool createschema) : EFStagedEntityRepository(opts) {
  
  public override async Task<IStagedEntityRepository> Initialise() {
    if (!createschema) return this;
    
    await using var db = opts.Db();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.StagedEntityTableName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], $"UNIQUE({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.StagedEntityChecksum)})"));
    var index = dbf.GenerateIndexScript(db.SchemaName, db.StagedEntityTableName, nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.DateStaged));
    await db.Database.ExecuteSqlRawAsync(index);
    return this;
  }

}