using Centazio.Core;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.CoreToSystemMapping;

public class SqliteCoreToSystemMapStore(Func<SqliteConnection> newconn) : AbstractCoreToSystemMapStore {

  protected const string MAPPING_TBL = $"{nameof(Core.Ctl)}_{nameof(Map.CoreToSystemMap)}";
  
  public async Task<ICoreToSystemMapStore> Initalise() {
    await using var conn = newconn();
    var dbf = new DbFieldsHelper();
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(MAPPING_TBL, dbf.GetDbFields<Map.CoreToSystemMap>(), 
        [nameof(Map.CoreToSystemMap.System), nameof(Map.CoreToSystemMap.CoreEntityTypeName), nameof(Map.CoreToSystemMap.CoreId)],
        $"UNIQUE({nameof(Map.CoreToSystemMap.System)}, {nameof(Map.CoreToSystemMap.CoreEntityTypeName)}, {nameof(Map.CoreToSystemMap.SystemId)})"));
    return this;
  }
  
  protected override async Task<List<Map.Created>> CreateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
INSERT INTO [{MAPPING_TBL}] (CoreEntityTypeName, CoreId, System, SystemId, SystemEntityChecksum, Status, DateCreated, DateUpdated, DateLastSuccess, DateLastError, LastError)
VALUES (@CoreEntityTypeName, @CoreId, @System, @SystemId, @SystemEntityChecksum, @Status, @DateCreated, @DateUpdated, @DateLastSuccess, @DateLastError, @LastError);", tocreate.Select(DtoHelpers.ToDto)); 
    
    var ids = (await conn.QueryAsync<CoreEntityId>(
        @$"SELECT CoreId FROM [{MAPPING_TBL}] 
            WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND DateCreated=@DateCreated", new { System=system, CoreEntityTypeName=coretype, tocreate.First().DateCreated }))
        .ToDictionary(id => id);
    return tocreate.Where(e => ids.ContainsKey(e.CoreId)).ToList();
  }

  // todo: this logic of returning the entities actually updated is good and should be replicated in StagedEntityStore also (and other stores)
  protected override async Task<List<Map.Updated>> UpdateImpl(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
UPDATE [{MAPPING_TBL}] 
SET SystemEntityChecksum=@SystemEntityChecksum, Status=@Status, DateUpdated=@DateUpdated, DateLastSuccess=@DateLastSuccess, DateLastError=@DateLastError, LastError=@LastError
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND CoreId=@CoreId AND SystemId=@SystemId", toupdate.Select(DtoHelpers.ToDto)); 
    
    var ids = (await conn.QueryAsync<CoreEntityId>(@$"SELECT CoreId FROM [{MAPPING_TBL}] WHERE DateUpdated=@DateUpdated", new { toupdate.First().DateUpdated })).ToDictionary(id => id);
    return toupdate.Where(e => ids.ContainsKey(e.CoreId)).ToList();
  }

  protected override async Task<List<Map.CoreToSystemMap>> GetExistingMapsByIds<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    var issysid = typeof(V) == typeof(SystemEntityId);
    var idfield = issysid ? nameof(Map.CoreToSystemMap.SystemId) : nameof(Map.CoreToSystemMap.CoreId);
    await using var conn = newconn();
    return (await Db.Query<Map.CoreToSystemMap.Dto>(conn, $@"
SELECT * FROM [{MAPPING_TBL}]   
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND {idfield} IN @Ids", new { System=system, CoreEntityTypeName=coretype, Ids=ids.Select(id => id.Value) }))
        .Select(dto => dto.ToBase()).ToList();
  }

  public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}