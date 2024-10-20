using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepository(Func<SqliteConnection> newconn) : ICtlRepository {

  internal static readonly string SYSTEM_STATE_TBL = $"{nameof(Core.Ctl)}_{nameof(SystemState)}".ToLower();
  internal static readonly string OBJECT_STATE_TBL = $"{nameof(Core.Ctl)}_{nameof(ObjectState)}".ToLower();

  public async Task<SqliteCtlRepository> Initalise() {
    await using var conn = newconn();
      var dbf = new DbFieldsHelper();
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(SYSTEM_STATE_TBL, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(OBJECT_STATE_TBL, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)], 
        $"FOREIGN KEY ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}]) REFERENCES [{SYSTEM_STATE_TBL}]([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])"));
    return this;
  }
  
  public async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<SystemState.Dto>($"SELECT * FROM {SYSTEM_STATE_TBL} WHERE System=@System AND Stage=@Stage", new { System=system, Stage=stage });
    return dto?.ToBase();
  }

  public async Task<SystemState> SaveSystemState(SystemState state) {
    await using var conn = newconn();
    var count = await Db.Exec(conn, $@"
UPDATE {SYSTEM_STATE_TBL} 
SET Active=@Active, Status=@Status, DateUpdated=@DateUpdated, LastStarted=@LastStarted, LastCompleted=@LastCompleted
WHERE System=@System AND Stage=@Stage", state);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var created = SystemState.Create(system, stage);
    await Db.Exec(conn, $@"
INSERT INTO {SYSTEM_STATE_TBL} 
(System, Stage, Active, Status, DateCreated)
VALUES (@System, @Stage, @Active, @Status, @DateCreated)", created);
    
    return created;
  }

  public async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    await using var conn = newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<ObjectState.Dto>(@$"
  SELECT * FROM {OBJECT_STATE_TBL} 
  WHERE System=@System AND Stage=@Stage AND Object=@Object",
        new { system.System, system.Stage, Object = obj });
    return dto?.ToBase();
  }

  public async Task<ObjectState> SaveObjectState(ObjectState state) {
    await using var conn = newconn();
    var count = await Db.Exec(conn, $@"
  UPDATE {OBJECT_STATE_TBL} 
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
    await using var conn = newconn();

    var created = ObjectState.Create(system.System, system.Stage, obj);
    await Db.Exec(conn, $@"
  INSERT INTO {OBJECT_STATE_TBL}
  (System, Stage, Object, ObjectIsCoreEntityType, ObjectIsSystemEntityType, Active, DateCreated, LastResult, LastAbortVote)
  VALUES (@System, @Stage, @Object, @ObjectIsCoreEntityType, @ObjectIsSystemEntityType, @Active, @DateCreated, @LastResult, @LastAbortVote)
  ", DtoHelpers.ToDto(created));

    return created;
  }

    public async ValueTask DisposeAsync() {
      await using var conn = newconn();
      await Db.Exec(conn, $"DROP TABLE IF EXISTS {OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SYSTEM_STATE_TBL};");
    }
}

