using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractStagedEntityRepositoryDbContext(StagedEntityRepositorySettings settings) : CentazioDbContext {
  protected StagedEntityRepositorySettings Settings => settings;
  
  public string SchemaName { get; } = settings.SchemaName;
  public string StagedEntityTableName { get; } = settings.TableName;
  
  public DbSet<StagedEntity> Staged { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => 
      builder
          .HasDefaultSchema(SchemaName)
          .Entity<StagedEntity>(e => e.ToTable(StagedEntityTableName));
}