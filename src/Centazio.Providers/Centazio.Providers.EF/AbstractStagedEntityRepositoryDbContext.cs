using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractStagedEntityRepositoryDbContext(string schema, string table) : CentazioDbContext {
  public string SchemaName { get; } = schema;
  public string StagedEntityTableName { get; } = table;
  
  public DbSet<StagedEntity.Dto> Staged { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => 
      builder
          .HasDefaultSchema(SchemaName)
          .Entity<StagedEntity.Dto>(e => e.ToTable(StagedEntityTableName));

  // todo: move create/drop table code to tests, not Centazio.Core
  public async Task CreateTableIfNotExists(IDbFieldsHelper dbf) {
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], $"UNIQUE({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.StagedEntityChecksum)})"));
    await Database.ExecuteSqlRawAsync(dbf.GenerateIndexScript(SchemaName, StagedEntityTableName, [nameof(StagedEntity.System), nameof(StagedEntity.SystemEntityTypeName), nameof(StagedEntity.DateStaged)]));
  }
  
  public async Task DropTable(IDbFieldsHelper dbf) {
    await Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(SchemaName, StagedEntityTableName));
  }
}