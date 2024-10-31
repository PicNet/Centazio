using Centazio.Core.CoreRepo;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public abstract class AbstractTestingCoreStorageDbContext(string schema) : CentazioDbContext {
  
  public string SchemaName { get; } = schema;
  public string CoreEntityName { get; } = nameof(CoreEntity).ToLower();
  public string CoreStorageMetaName { get; } = nameof(CoreStorageMeta).ToLower();
  
  public DbSet<CoreEntity.Dto> CoreEntities { get; set; }
  public DbSet<CoreStorageMeta.Dto> Metas { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(SchemaName)
      .Entity<CoreEntity.Dto>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName);
        e.HasKey(e2 => e2.CoreId);
      });
}