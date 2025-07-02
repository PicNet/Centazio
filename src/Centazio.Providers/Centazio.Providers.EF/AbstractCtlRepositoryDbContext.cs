using Centazio.Core.Ctl.Entities;
using Centazio.Core.Settings;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractCtlRepositoryDbContext(CtlRepositorySettings settings) : CentazioDbContext {
  
  public CtlRepositorySettings Settings => settings;
  
  public DbSet<SystemState> SystemStates { get; set; }
  public DbSet<ObjectState> ObjectStates { get; set; }
  public DbSet<Map.CoreToSysMap> CoreToSystemMaps { get; set; }
  public DbSet<EntityChange> EntityChanges { get; set; }
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(Settings.SchemaName)
      .Entity<SystemState>(e => {
        e.ToTable(Settings.SystemStateTableName);
        e.HasKey(nameof(SystemState.System), nameof(SystemState.Stage));
      })
      .Entity<ObjectState>(e => {
        e.ToTable(Settings.ObjectStateTableName);
        e.HasKey(nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object));
      })
      .Entity<Map.CoreToSysMap>(e => {
        e.ToTable(Settings.CoreToSysMapTableName);
        e.HasKey(nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId));
      })
      .Entity<EntityChange>(e => {
        e.ToTable(Settings.EntityChangeTableName);
        e.HasKey(nameof(EntityChange.CoreEntityTypeName), nameof(EntityChange.CoreId), nameof(EntityChange.ChangeDate));
      });
}