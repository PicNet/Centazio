using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF;

public abstract class AbstractCtlRepositoryDbContext(string schema, string sys_tbl, string obj_tbl, string map_tbl) : CentazioDbContext {
  public string SchemaName { get; } = schema;
  public string SystemStateTableName { get; } = sys_tbl;
  public string ObjectStateTableName { get; } = obj_tbl;
  public string CoreToSystemMapTableName { get; } = map_tbl;
  
  public DbSet<SystemState.Dto> SystemStates { get; set; }
  public DbSet<ObjectState.Dto> ObjectStates { get; set; }
  public DbSet<Map.CoreToSysMap.Dto> CoreToSystemMaps { get; set; }
  
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
      });

  // todo: create/drop table code should be part of tests, not Centazio.Core
  public async Task CreateTableIfNotExists(IDbFieldsHelper dbf) {
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, SystemStateTableName, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, ObjectStateTableName, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)], 
        $"FOREIGN KEY ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}]) REFERENCES [{SystemStateTableName}]([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])"));
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, CoreToSystemMapTableName, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        $"UNIQUE({nameof(Map.CoreToSysMap.System)}, {nameof(Map.CoreToSysMap.CoreEntityTypeName)}, {nameof(Map.CoreToSysMap.SystemId)})"));
  }
  
  public async Task DropTables() {
    #pragma warning disable EF1002
    await Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {CoreToSystemMapTableName}; DROP TABLE IF EXISTS {ObjectStateTableName}; DROP TABLE IF EXISTS {SystemStateTableName};");
    #pragma warning restore EF1002
  }

}