using Centazio.Core.Ctl.Entities;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractCtlRepositoryDbContext(string schema, string sys_tbl, string obj_tbl, string map_tbl, string change_tbl) : CentazioDbContext {
  public string SchemaName { get; } = schema;
  public string SystemStateTableName { get; } = sys_tbl;
  public string ObjectStateTableName { get; } = obj_tbl;
  public string CoreToSystemMapTableName { get; } = map_tbl;
  public string EntityChangeTableName { get; } = change_tbl;
  
  public DbSet<SystemState.Dto> SystemStates { get; set; }
  public DbSet<ObjectState.Dto> ObjectStates { get; set; }
  public DbSet<Map.CoreToSysMap.Dto> CoreToSystemMaps { get; set; }
  public DbSet<EntityChange.Dto> EntityChanges { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(SchemaName)
      .Entity<SystemState.Dto>(e => {
        e.ToTable(SystemStateTableName);
        e.HasKey(nameof(SystemState.System), nameof(SystemState.Stage));
      })
      .Entity<ObjectState.Dto>(e => {
        e.ToTable(ObjectStateTableName);
        e.HasKey(nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object));
      })
      .Entity<Map.CoreToSysMap.Dto>(e => {
        e.ToTable(CoreToSystemMapTableName);
        e.HasKey(nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId));
      })
      .Entity<EntityChange.Dto>(e => {
        e.ToTable(EntityChangeTableName);
        e.HasKey(nameof(EntityChange.CoreEntityTypeName), nameof(EntityChange.CoreId), nameof(EntityChange.ChangeDate));
      });
}