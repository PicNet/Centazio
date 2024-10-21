﻿using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepository(Func<Task<SqlConnection>> newconn) : BaseCtlRepository {

  internal static readonly string SCHEMA = nameof(Core.Ctl).ToLower();
  internal static readonly string SYSTEM_STATE_TBL = nameof(SystemState).ToLower();
  internal static readonly string OBJECT_STATE_TBL = nameof(ObjectState).ToLower();
  internal static readonly string MAPPING_TBL = nameof(Map.CoreToSysMap).ToLower();

  public async Task<SqlServerCtlRepository> Initalise() {
    await using var conn = await newconn();
    var dbf = new DbFieldsHelper();
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, SYSTEM_STATE_TBL, dbf.GetDbFields<SystemState>(), 
        [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, OBJECT_STATE_TBL, dbf.GetDbFields<ObjectState>(), 
        [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)], 
        $"FOREIGN KEY ([{nameof(ObjectState.System)}], [{nameof(ObjectState.Stage)}]) REFERENCES [{SCHEMA}].[{SYSTEM_STATE_TBL}]([{nameof(ObjectState.System)}], [{nameof(ObjectState.Stage)}])"));
    // todo: remove if qoeks
    /*await conn.ExecuteAsync($@"ALTER TABLE [{SCHEMA}].[{OBJECT_STATE_TBL}]
  ADD CONSTRAINT FK_{nameof(ObjectState)}_{nameof(SystemState)} FOREIGN KEY ([{nameof(ObjectState.System)}], [{nameof(ObjectState.Stage)}])
  REFERENCES [{SCHEMA}].[{SYSTEM_STATE_TBL}] ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])");
  */
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, MAPPING_TBL, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)], 
        $"CONSTRAINT UNIQUE_SystemId UNIQUE ({nameof(Map.CoreToSysMap.System)}, {nameof(Map.CoreToSysMap.CoreEntityTypeName)}, {nameof(Map.CoreToSysMap.SystemId)})"));
    return this;
  }
  
  public override async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = await newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<SystemState.Dto>($"SELECT * FROM {SCHEMA}.{SYSTEM_STATE_TBL} WHERE System=@System AND Stage=@Stage", new { System=system, Stage=stage });
    return dto?.ToBase();
  }

  public override async Task<SystemState> SaveSystemState(SystemState state) {
    await using var conn = await newconn();
    var count = await conn.ExecuteAsync($@"
UPDATE {SCHEMA}.{SYSTEM_STATE_TBL} 
SET Active=@Active, Status=@Status, DateUpdated=@DateUpdated, LastStarted=@LastStarted, LastCompleted=@LastCompleted
WHERE System=@System AND Stage=@Stage", state);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public override async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = await newconn();
    var created = SystemState.Create(system, stage);
    await conn.ExecuteAsync($@"
INSERT INTO {SCHEMA}.{SYSTEM_STATE_TBL} 
(System, Stage, Active, Status, DateCreated)
VALUES (@System, @Stage, @Active, @Status, @DateCreated)", created);
    
    return created;
  }

  public override async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    await using var conn = await newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<ObjectState.Dto>(@$"
  SELECT * FROM {SCHEMA}.{OBJECT_STATE_TBL} 
  WHERE System=@System AND Stage=@Stage AND Object=@Object",
        new { system.System, system.Stage, Object = obj });
    return dto?.ToBase();
  }

  public override async Task<ObjectState> SaveObjectState(ObjectState state) {
    await using var conn = await newconn();
    var count = await conn.ExecuteAsync($@"
  UPDATE {SCHEMA}.{OBJECT_STATE_TBL} 
  SET 
    Active=@Active, 
    LastResult=@LastResult, 
    LastAbortVote=@LastAbortVote, 
    DateUpdated=@DateUpdated, 
    LastStart=@LastStart,  
    LastCompleted=@LastCompleted,
    LastSuccessStart=@LastSuccessStart,
    LastSuccessCompleted=@LastSuccessCompleted,
    LastRunMessage=@LastRunMessage, 
    LastRunException=@LastRunException 
  WHERE System=@System AND Stage=@Stage AND Object=@Object", state);
    return count == 0 ? throw new Exception("SaveObjectState failed") : state;
  }

  public override async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj) {
    await using var conn = await newconn();

    var created = ObjectState.Create(system.System, system.Stage, obj);
    await conn.ExecuteAsync($@"
  INSERT INTO {SCHEMA}.{OBJECT_STATE_TBL}
  (System, Stage, Object, ObjectIsCoreEntityType, ObjectIsSystemEntityType, Active, DateCreated, LastResult, LastAbortVote)
  VALUES (@System, @Stage, @Object, @ObjectIsCoreEntityType, @ObjectIsSystemEntityType, @Active, @DateCreated, @LastResult, @LastAbortVote)
  ", created);

    return created;
  }
  
  // todo: implement SqlServer CoreToSystemMap code
  protected override Task<List<Map.Created>> CreateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) => throw new NotImplementedException();
  protected override Task<List<Map.Updated>> UpdateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) => throw new NotImplementedException();
  protected override Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) => throw new NotImplementedException();

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }
  
}

