using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public abstract class AbstractTestingCoreStorageDbContext(string schema) : CentazioDbContext {
  
  public string SchemaName { get; } = schema;
  public string CoreEntityName { get; } = nameof(CoreEntity).ToLower();
  
  public DbSet<CoreEntity.Dto> CoreEntities { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(SchemaName)
      .Entity<CoreEntity.Dto>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      });
}