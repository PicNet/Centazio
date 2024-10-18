using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.CoreToSystemMapping;

// todo: abstract common logic to AbstractCoreToSystemMapStore
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
  
  public override async Task<List<Map.Created>> Create(SystemName system, CoreEntityTypeName coretype, List<Map.Created> tocreate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
INSERT INTO [{MAPPING_TBL}] (CoreEntityTypeName, CoreId, System, SystemId, SystemEntityChecksum, Status, DateCreated, DateUpdated, DateLastSuccess, DateLastError, LastError)
VALUES (@CoreEntityTypeName, @CoreId, @System, @SystemId, @SystemEntityChecksum, @Status, @DateCreated, @DateUpdated, @DateLastSuccess, @DateLastError, @LastError);", tocreate.Select(DtoHelpers.ToDto)); 
    
    // all DateCreated are the same for this batch, so use that as the batch identifier
    // todo: ensure this is the case by adding validation in the abstract class, or make it impossible to violate
    // todo: also ensure that all entities in this batch are for the same System and CoreEntityTypeName
    // todo: and ensure that no duplicate CoreIds are in the batch
    var ids = (await conn.QueryAsync<CoreEntityId>(
        @$"SELECT CoreId FROM [{MAPPING_TBL}] 
            WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND DateCreated=@DateCreated", new { System=system, CoreEntityTypeName=coretype, tocreate.First().DateCreated }))
        .ToDictionary(id => id);

    var duplicates = tocreate.Where(e => !ids.ContainsKey(e.CoreId)).ToList();
    if (duplicates.Any()) throw new Exception($"attempted to add duplicate SystemIDs to CoreToSystemMapStore: " + String.Join(",", duplicates.Select(e => e.SystemId)));
    return tocreate;
  }

  // todo: this logic of returning the entities actually updated is good and should be replicated in StagedEntityStore also (and other stores)
  public override async Task<List<Map.Updated>> Update(SystemName system, CoreEntityTypeName coretype, List<Map.Updated> toupdate) {
    await using var conn = newconn();
    await Db.Exec(conn, $@"
UPDATE [{MAPPING_TBL}] 
SET SystemEntityChecksum=@SystemEntityChecksum, Status=@Status, DateUpdated=@DateUpdated, DateLastSuccess=@DateLastSuccess, DateLastError=@DateLastError, LastError=@LastError
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND CoreId=@CoreId", toupdate.Select(DtoHelpers.ToDto)); 
    
    // all DateUpdated are the same for this batch, so use that as the batch identifier
    // todo: ensure this is the case by adding validation in the abstract class, or make it impossible to violate
    // todo: also ensure that all entities in this batch are for the same System and CoreEntityTypeName
    // todo: and ensure that no duplicate CoreIds are in the batch
    var ids = (await conn.QueryAsync<CoreEntityId>(@$"SELECT CoreId FROM [{MAPPING_TBL}] WHERE DateUpdated=@DateUpdated", new { toupdate.First().DateUpdated })).ToDictionary(id => id);
    return toupdate.Where(e => ids.ContainsKey(e.CoreId)).ToList();
  }

  public override async Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(SystemName system, List<ICoreEntity> coreents) {
    var (news, updates) = (new List<CoreAndPendingCreateMap>(), new List<CoreAndPendingUpdateMap>());
    if (!coreents.Any()) return (news, updates);
    var first = coreents[0];
    var typenm = CoreEntityTypeName.From(first);
    // todo: add validation that all entities are of same type
    await using var conn = newconn();
    var maps = (await Db.Query<Map.CoreToSystemMap.Dto>(conn, $@"
SELECT * FROM [{MAPPING_TBL}] WHERE  
CoreEntityTypeName=@CoreEntityTypeName AND System=@System AND CoreId IN(@CoreIds)", new { CoreEntityTypeName=typenm, CoreIds=coreents.Select(e => e.CoreId.Value), System=system })) // todo: we should not need these '.Values' here, check everywhere we use dapper
            .Select(dto => dto.ToBase()).ToList();
    
    // todo: logic below should be abstracted to base class, query is the only thing that changes in proviers
    coreents.ForEach(c => {
      var map = maps.SingleOrDefault(kvp => kvp.Key.CoreEntityTypeName == CoreEntityTypeName.From(c) && kvp.Key.CoreId == c.CoreId && kvp.Key.System == system);
      if (map is null) news.Add(new CoreAndPendingCreateMap(c, Map.Create(system, c)));
      else updates.Add(new CoreAndPendingUpdateMap(c, map.Update()));
    });
    return (news, updates);
  }

  protected override async Task<List<Map.CoreToSystemMap>> GetById<V>(SystemName system, CoreEntityTypeName coretype, List<V> ids) {
    // todo: add Distinct filter to ids in base class
    var issysid = typeof(V) == typeof(SystemEntityId);
    var idfield = issysid ? nameof(Map.CoreToSystemMap.SystemId) : nameof(Map.CoreToSystemMap.CoreId);
    await using var conn = newconn();
    // todo: shoudl Db.Query automaticall convert to base obj?
    return (await Db.Query<Map.CoreToSystemMap.Dto>(conn, $@"
SELECT * FROM [{MAPPING_TBL}]   
WHERE System=@System AND CoreEntityTypeName=@CoreEntityTypeName AND {idfield} IN @Ids", new { System=system.Value, CoreEntityTypeName=coretype.Value, Ids=ids.Select(id => id.Value) }))
        .Select(dto => dto.ToBase()).ToList();
  }

  public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}