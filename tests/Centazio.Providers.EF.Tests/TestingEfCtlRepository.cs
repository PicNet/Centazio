using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests;

public class TestingEfCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf) : EFCtlRepository(getdb), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var db = getdb();
    return (await db.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
  
  public override async Task<ICtlRepository> Initialise() {
    await using var db = getdb();
    await DropTablesImpl(db);
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.SystemStateTableName, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.ObjectStateTableName, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)],
        [],
        [new ForeignKey([nameof(SystemState.System), nameof(SystemState.Stage)], db.SchemaName, db.SystemStateTableName)]));
    await db.ExecSql(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreToSystemMapTableName, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        [[nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.SystemId)]]));
    
    return await base.Initialise();
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = getdb();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(AbstractCtlRepositoryDbContext db) { 
    await db.ExecSql(dbf.GenerateDropTableScript(db.SchemaName, db.CoreToSystemMapTableName));
    await db.ExecSql(dbf.GenerateDropTableScript(db.SchemaName, db.ObjectStateTableName));
    await db.ExecSql(dbf.GenerateDropTableScript(db.SchemaName, db.SystemStateTableName));
  }

}