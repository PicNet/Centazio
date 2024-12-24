using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepositoryFactory(CentazioSettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() {
    var ctlsetts = settings.CtlRepository;
    return new SqliteCtlRepository(Getdb, new SqliteDbFieldsHelper(), ctlsetts.CreateSchema);
    
    AbstractCtlRepositoryDbContext Getdb() => new SqliteCtlRepositoryDbContext(ctlsetts.ConnectionString, ctlsetts.SchemaName, ctlsetts.SystemStateTableName, ctlsetts.ObjectStateTableName, ctlsetts.CoreToSysMapTableName);
  }
}

public class SqliteCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf, bool createschema) : EFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    await using var db = getdb();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.SystemStateTableName, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.ObjectStateTableName, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)], 
        $"FOREIGN KEY ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}]) REFERENCES {dbf.TableName(db.SchemaName, db.SystemStateTableName)}([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])"));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreToSystemMapTableName, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        $"UNIQUE({nameof(Map.CoreToSysMap.System)}, {nameof(Map.CoreToSysMap.CoreEntityTypeName)}, {nameof(Map.CoreToSysMap.SystemId)})"));
    return this;
  }

}