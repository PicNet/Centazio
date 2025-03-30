using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepositoryFactory(CtlRepositorySettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => 
      new SqlServerCtlRepository(Getdb, new SqlServerDbFieldsHelper(), settings.CreateSchema);
  
  private AbstractCtlRepositoryDbContext Getdb() 
    => new SqlServerCtlRepositoryDbContext(settings.ConnectionString, settings.SchemaName, settings.SystemStateTableName, settings.ObjectStateTableName, settings.CoreToSysMapTableName);
}

public class SqlServerCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf, bool createschema) : EFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    await using var db = getdb();
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.SystemStateTableName, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.ObjectStateTableName, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)],
        [],
        [new ForeignKey([nameof(SystemState.System), nameof(SystemState.Stage)], db.SchemaName, db.SystemStateTableName)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreToSystemMapTableName, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        [[nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.SystemId)]]));
    return this;
  }

}