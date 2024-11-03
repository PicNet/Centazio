using Centazio.Core.CoreRepo;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public abstract class AbstractTestingCoreStorageDbContext(string coreschema, string ctlschema) : CentazioDbContext {

  public static readonly string DEFAULT_CORE_SCHEMA_NAME = "dbo";
  
  public string CoreSchemaName { get; } = coreschema;
  public string CtlSchemaName { get; } = ctlschema;
  public string CoreEntityName { get; } = nameof(CoreEntity).ToLower();
  public string CoreStorageMetaName { get; } = nameof(CoreStorageMeta).ToLower();
  
  public DbSet<CoreEntity.Dto> CoreEntities { get; set; }

  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(CoreSchemaName)
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName, CtlSchemaName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreEntity.Dto>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      });

  public DbSet<CoreStorageMeta.Dto> Metas { get; set; }

}