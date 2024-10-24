using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepository(Func<SqliteConnection> newconn) : AbstractCtlRepository {

  internal static readonly string SYSTEM_STATE_TBL = $"{nameof(Core.Ctl)}_{nameof(SystemState)}".ToLower();
  internal static readonly string OBJECT_STATE_TBL = $"{nameof(Core.Ctl)}_{nameof(ObjectState)}".ToLower();
  internal static readonly string MAPPING_TBL = $"{nameof(Core.Ctl)}_{nameof(Map.CoreToSysMap)}".ToLower();
  
  public async Task<SqliteCtlRepository> Initalise() {
    await using var conn = newconn();
    var dbf = new DbFieldsHelper();
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(SYSTEM_STATE_TBL, dbf.GetDbFields<SystemState>(), [nameof(SystemState.System), nameof(SystemState.Stage)]));
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(OBJECT_STATE_TBL, dbf.GetDbFields<ObjectState>(), [nameof(ObjectState.System), nameof(ObjectState.Stage), nameof(ObjectState.Object)], 
        $"FOREIGN KEY ([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}]) REFERENCES [{SYSTEM_STATE_TBL}]([{nameof(SystemState.System)}], [{nameof(SystemState.Stage)}])"));
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(MAPPING_TBL, dbf.GetDbFields<Map.CoreToSysMap>(), 
        [nameof(Map.CoreToSysMap.System), nameof(Map.CoreToSysMap.CoreEntityTypeName), nameof(Map.CoreToSysMap.CoreId)],
        $"UNIQUE({nameof(Map.CoreToSysMap.System)}, {nameof(Map.CoreToSysMap.CoreEntityTypeName)}, {nameof(Map.CoreToSysMap.SystemId)})"));
    return this;
  }
  
  public override async Task<SystemState?> GetSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<SystemState.Dto>($"SELECT * FROM {SYSTEM_STATE_TBL} WHERE System=@System AND Stage=@Stage", new { System=system, Stage=stage });
    return dto?.ToBase();
  }

  public override async Task<SystemState> SaveSystemState(SystemState state) {
    await using var conn = newconn();
    var count = await Db.Exec(conn, $@"
UPDATE {SYSTEM_STATE_TBL} 
SET Active=@Active, Status=@Status, DateUpdated=@DateUpdated, LastStarted=@LastStarted, LastCompleted=@LastCompleted
WHERE System=@System AND Stage=@Stage", state);
    return count == 0 ? throw new Exception("SaveSystemState failed") : state;
  }

  public override async Task<SystemState> CreateSystemState(SystemName system, LifecycleStage stage) {
    await using var conn = newconn();
    var created = SystemState.Create(system, stage);
    await Db.Exec(conn, $@"
INSERT INTO {SYSTEM_STATE_TBL} 
(System, Stage, Active, Status, DateCreated)
VALUES (@System, @Stage, @Active, @Status, @DateCreated)", created);
    
    return created;
  }

  public override async Task<ObjectState?> GetObjectState(SystemState system, ObjectName obj) {
    await using var conn = newconn();
    var dto = await conn.QuerySingleOrDefaultAsync<ObjectState.Dto>(@$"
  SELECT * FROM {OBJECT_STATE_TBL} 
  WHERE System=@System AND Stage=@Stage AND Object=@Object",
        new { system.System, system.Stage, Object = obj });
    return dto?.ToBase();
  }

  public override async Task<ObjectState> SaveObjectState(ObjectState state) {
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

  public override async Task<ObjectState> CreateObjectState(SystemState system, ObjectName obj) {
    await using var conn = newconn();

    var created = ObjectState.Create(system.System, system.Stage, obj);
    await Db.Exec(conn, $@"
  INSERT INTO {OBJECT_STATE_TBL}
  (System, Stage, Object, ObjectIsCoreEntityType, ObjectIsSystemEntityType, Active, DateCreated, LastResult, LastAbortVote)
  VALUES (@System, @Stage, @Object, @ObjectIsCoreEntityType, @ObjectIsSystemEntityType, @Active, @DateCreated, @LastResult, @LastAbortVote)
  ", DtoHelpers.ToDto(created));

    return created;
  }
  
  protected override async Task<List<Map.Created>> CreateMapImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
INSERT INTO [{MAPPING_TBL}] (CoreEntityTypeName, CoreId, System, SystemId, SystemEntityChecksum, Status, DateCreated, DateUpdated, DateLastSuccess, DateLastError, LastError)
VALUES (@CoreEntityTypeName, @CoreId, @System, @SystemId, @SystemEntityChecksum, @Status, @DateCreated, @DateUpdated, @DateLastSuccess, @DateLastError, @LastError);", tocreate.Select(DtoHelpers.ToDto)); 
    
    var ids = (await Db.Query<CoreEntityId>(conn,
        @$"SELECT CoreId FROM [{MAPPING_TBL}] 
            WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND DateCreated=@DateCreated", new { System=system, CoreEntityTypeName=coretype, tocreate.First().DateCreated }))
        .ToDictionary(id => id);
    return tocreate.Where(e => ids.ContainsKey(e.CoreId)).ToList();
  }

  protected override async Task<List<Map.Updated>> UpdateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
UPDATE [{MAPPING_TBL}] 
SET SystemEntityChecksum=@SystemEntityChecksum, Status=@Status, DateUpdated=@DateUpdated, DateLastSuccess=@DateLastSuccess, DateLastError=@DateLastError, LastError=@LastError
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND CoreId=@CoreId AND SystemId=@SystemId", toupdate.Select(DtoHelpers.ToDto)); 
    
    var ids = (await Db.Query<CoreEntityId>(conn, $"SELECT CoreId FROM [{MAPPING_TBL}] WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND DateUpdated=@DateUpdated", 
        new { System=system, CoreEntityTypeName=coretype, toupdate.First().DateUpdated })).ToDictionary(id => id);
    return toupdate.Where(e => ids.ContainsKey(e.CoreId)).ToList();
  }

  protected override async Task<List<Map.CoreToSysMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysid = typeof(V) == typeof(SystemEntityId);
    var idfield = issysid ? nameof(Map.CoreToSysMap.SystemId) : nameof(Map.CoreToSysMap.CoreId);
    await using var conn = newconn();
    return (await Db.Query<Map.CoreToSysMap.Dto>(conn, $@"
SELECT * FROM [{MAPPING_TBL}]   
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND {idfield} IN @Ids", new { System=system, CoreEntityTypeName=coretype, Ids=ids.Select(id => id.Value) }))
        .Select(dto => dto.ToBase()).ToList();
  }

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

}

