using Centazio.Core.Ctl.Entities;
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
}