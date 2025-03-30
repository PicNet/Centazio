using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings, IChecksumAlgorithm checksum) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EFStagedEntityRepositoryOptions(
        settings.Limit,
        checksum.Checksum, 
        () => new SqlServerStagedEntityContext(settings.ConnectionString, settings.SchemaName, settings.TableName));
    return new SqlServerStagedEntityRepository(opts, new SqlServerDbFieldsHelper(), settings.CreateSchema);
  }
}

public class SqlServerStagedEntityRepository(EFStagedEntityRepositoryOptions opts, IDbFieldsHelper dbf, bool createschema) : EFStagedEntityRepository(opts) {
  public override async Task<IStagedEntityRepository> Initialise() {
    if (!createschema) return this;
    
    await using var db = opts.Db();
    await db.ExecSql(dbf.GenerateDropTableScript(db.SchemaName, db.StagedEntityTableName));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)],
        [[nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.StagedEntityChecksum)]]));
    var index = dbf.GenerateIndexScript(db.SchemaName, db.StagedEntityTableName, nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.DateStaged));
    await db.ExecSql(index);
    return this;
  }

}