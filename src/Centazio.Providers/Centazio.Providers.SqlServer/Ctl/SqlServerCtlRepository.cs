using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepository(Func<Task<SqlConnection>> newconn) : ICtlRepository {

  internal static readonly string SCHEMA = nameof(Core.Ctl).ToLower();
  internal const string SYSTEM_STATE_TBL = nameof(SystemState);
  internal const string OBJECT_STATE_TBL = nameof(ObjectState);

  public async Task<SqlServerCtlRepository> Initalise() {
    await using var conn = await newconn();
    var dbf = new DbFieldsHelper();
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, SYSTEM_STATE_TBL, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, OBJECT_STATE_TBL, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)]));
    await conn.ExecuteAsync($@"
ALTER TABLE [{SCHEMA}].[{OBJECT_STATE_TBL}]
  ADD CONSTRAINT FK_{nameof(ObjectState)}_{nameof(SystemState)} FOREIGN KEY ([{nameof(ObjectState.System)}], [{nameof(ObjectState.Stage)}])
  REFERENCES [{SCHEMA}].[{SYSTEM_STATE_TBL}] ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])");
    return this;
  }
  
  public async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = await newconn();
    var raw = await conn.QuerySingleOrDefaultAsync<SystemState.Dto>($"SELECT * FROM {SCHEMA}.{SYSTEM_STATE_TBL} WHERE System=@System AND Stage=@Stage", new { System=system, Stage=stage });
    return raw?.ToBase();
  }

  public async Task<SystemState> SaveSystemState(SystemState state) {
    await using var conn = await newconn();
    var count = await conn.ExecuteAsync($@"
UPDATE {SCHEMA}.{SYSTEM_STATE_TBL} 
SET Active=@Active, Status=@Status, DateUpdated=@DateUpdated, LastStarted=@LastStarted, LastCompleted=@LastCompleted
WHERE System=@System AND Stage=@Stage", state);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = await newconn();
    var created = SystemState.Create(system, stage);
    await conn.ExecuteAsync($@"
INSERT INTO {SCHEMA}.{SYSTEM_STATE_TBL} 
(System, Stage, Active, Status, DateCreated)
VALUES (@System, @Stage, @Active, @Status, @DateCreated)", created);
    
    return created;
  }

  public async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    await using var conn = await newconn();
    var raw = await conn.QuerySingleOrDefaultAsync<ObjectState.Dto>(@$"
  SELECT * FROM {SCHEMA}.{OBJECT_STATE_TBL} 
  WHERE System=@System AND Stage=@Stage AND Object=@Object",
        new { system.System, system.Stage, Object = obj });
    return raw?.ToBase();
  }

  public async Task<ObjectState> SaveObjectState(ObjectState state) {
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

  public async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj) {
    await using var conn = await newconn();

    var created = ObjectState.Create(system.System, system.Stage, obj);
    await conn.ExecuteAsync($@"
  INSERT INTO {SCHEMA}.{OBJECT_STATE_TBL}
  (System, Stage, Object, ObjectIsCoreEntityType, ObjectIsSystemEntityType, Active, DateCreated, LastResult, LastAbortVote)
  VALUES (@System, @System, @Object, @ObjectIsCoreEntityType, @ObjectIsSystemEntityType, @Active, @DateCreated, @LastResult, @LastAbortVote)
  ", created);

    return created;
  }

  public virtual ValueTask DisposeAsync() { return ValueTask.CompletedTask; }
  
}

