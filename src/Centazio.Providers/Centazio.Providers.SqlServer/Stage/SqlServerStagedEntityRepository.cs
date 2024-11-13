using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityRepositoryFactory(CentazioSettings settings) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var sesetts = settings.StagedEntityRepository;
    var opts = new EFStagedEntityRepositoryOptions(
        sesetts.Limit, 
        new Sha256ChecksumAlgorithm().Checksum, 
        () => new SqlServerStagedEntityContext(sesetts.ConnectionString, sesetts.SchemaName, sesetts.TableName));
    return new SqlServerStagedEntityRepository(opts, new SqlServerDbFieldsHelper(), sesetts.CreateSchema);
  }
}

public class SqlServerStagedEntityRepository(EFStagedEntityRepositoryOptions opts, IDbFieldsHelper dbf, bool createschema) : EFStagedEntityRepository(opts) {
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