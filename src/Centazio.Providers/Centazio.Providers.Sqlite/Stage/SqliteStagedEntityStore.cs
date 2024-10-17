using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityStore(Func<SqliteConnection> newconn, int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityStore(limit, checksum) {
  
  internal static readonly string STAGED_ENTITY_TBL = $"{nameof(Core.Ctl)}_{nameof(StagedEntity)}";

  public async Task<SqliteStagedEntityStore> Initalise() {
    await using var conn = newconn();
    var dbf = new DbFieldsHelper();
    await Exec(conn, dbf.GetSqliteCreateTableScript(STAGED_ENTITY_TBL, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)], "UNIQUE(System, SystemEntityTypeName, StagedEntityChecksum)"));
    await Exec(conn, $"CREATE INDEX ix_{STAGED_ENTITY_TBL}_source_obj_staged ON [{STAGED_ENTITY_TBL}] ({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.DateStaged)});");
    return this;
  }
  
  public override async ValueTask DisposeAsync() {
    await using var conn = newconn();
    await Exec(conn, $"DROP TABLE IF EXISTS {STAGED_ENTITY_TBL}");
  }

  protected override async Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    // all staged entities will have the same DateStaged so just use first as the id of this bulk insert bacth
    var dtstaged = staged.First().DateStaged;
    await using var conn = newconn();
    
    await Exec(conn, $@"
INSERT INTO [{STAGED_ENTITY_TBL}] (Id, System, SystemEntityTypeName, DateStaged, Data, StagedEntityChecksum)
VALUES (@Id, @System, @SystemEntityTypeName, @DateStaged, @Data, @StagedEntityChecksum)
ON CONFLICT (System, SystemEntityTypeName, StagedEntityChecksum) DO NOTHING;
", staged.Select(e => e.ToDto()));
    
    var ids = (await conn.QueryAsync<Guid>($"SELECT Id FROM [{STAGED_ENTITY_TBL}] WHERE DateStaged=@DateStaged", new { DateStaged = dtstaged })).ToDictionary(id => id);
    return staged.Where(e => ids.ContainsKey(e.Id)).ToList();
  }

  public override async Task Update(List<StagedEntity> staged) {
    await using var conn = newconn();
    await Exec(conn, 
        $@"UPDATE [{STAGED_ENTITY_TBL}] 
SET DatePromoted=@DatePromoted, IgnoreReason=@IgnoreReason
WHERE System=@System AND SystemEntityTypeName=@SystemEntityTypeName AND Id=@Id;", staged);
  }

  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    await using var conn = newconn();
    var limit = Limit is > 0 and < Int32.MaxValue ? $"LIMIT {Limit}" : String.Empty;
    var promotedpredicate = incpromoted ? String.Empty : "AND DatePromoted IS NULL";
    var sql = @$"
SELECT * 
FROM [{STAGED_ENTITY_TBL}] 
WHERE 
  DateStaged > @after
  AND System = @system
  AND SystemEntityTypeName = @systype  
  AND IgnoreReason IS NULL 
  {promotedpredicate} 
ORDER BY DateStaged
{limit}
";
    return (await conn.QueryAsync<StagedEntity.Dto>(sql, new { after, system, systype }))
        .Select(e => e.ToBase()).ToList();
  }

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    await using var conn = newconn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await Exec(conn, $"DELETE FROM [{STAGED_ENTITY_TBL}] WHERE {col} < @before AND System = @system AND SystemEntityTypeName = @systype", new { before, system, systype });
  }
  
  private Task<int> Exec(SqliteConnection conn, string sql, object? arg = null) => conn.ExecuteAsync(sql, arg);

}

