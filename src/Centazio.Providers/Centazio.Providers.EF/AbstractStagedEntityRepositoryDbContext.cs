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

  // todo: code below is db specific
  public async Task CreateTableIfNotExists() {
    var dbf = new DbFieldsHelper();
    // todo: this use of `table` does not handle schema and will fail in Sql Server
    await Database.ExecuteSqlRawAsync(dbf.GetSqliteCreateTableScript(StagedEntityTableName, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], $"UNIQUE({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.StagedEntityChecksum)})"));
    #pragma warning disable EF1002
    await Database.ExecuteSqlRawAsync($"CREATE INDEX IF NOT EXISTS ix_{StagedEntityTableName}_source_obj_staged ON [{StagedEntityTableName}] ({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.DateStaged)});");
    #pragma warning restore EF1002
  }
  
  public async Task DropTable() {
    #pragma warning disable EF1002
    await Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {StagedEntityTableName}");
    #pragma warning restore EF1002
  }
}