using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityRepository(Func<Task<SqlConnection>> newconn, int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityRepository(limit, checksum) {

  internal static readonly string SCHEMA = nameof(Core.Ctl).ToLower();
  internal const string STAGED_ENTITY_TBL = nameof(StagedEntity);

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

  public async Task<SqlServerStagedEntityRepository> Initalise() {
    await using var conn = await newconn();
    var dbf = new DbFieldsHelper();
    await conn.ExecuteAsync(dbf.GetSqlServerCreateTableScript(SCHEMA, STAGED_ENTITY_TBL, dbf.GetDbFields<StagedEntity>(), [nameof(StagedEntity.Id)]));
    await conn.ExecuteAsync($@"
ALTER TABLE [{SCHEMA}].[{STAGED_ENTITY_TBL}] REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
CREATE INDEX ix_{STAGED_ENTITY_TBL}_source_obj_staged ON [{SCHEMA}].[{STAGED_ENTITY_TBL}] ({nameof(StagedEntity.System)}, {nameof(StagedEntity.SystemEntityTypeName)}, {nameof(StagedEntity.DateStaged)});");
    return this;
  }

  protected override async Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    // all staged entities will have the same DateStaged so just use first as the id of this bulk insert batch
    var dtstaged = staged.First().DateStaged;
    await using var conn = await newconn();
    await conn.ExecuteAsync($@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @System, @SystemEntityTypeName, @DateStaged, @Data, @StagedEntityChecksum))
  AS se (Id, System, SystemEntityTypeName, DateStaged, Data, StagedEntityChecksum)
ON 
  T.System = se.System
  AND T.SystemEntityTypeName = se.SystemEntityTypeName
  AND T.StagedEntityChecksum = se.StagedEntityChecksum
WHEN NOT MATCHED THEN
 INSERT (Id, System, SystemEntityTypeName, DateStaged, Data, StagedEntityChecksum)
 VALUES (se.Id, se.System, se.SystemEntityTypeName, se.DateStaged, se.Data, se.StagedEntityChecksum)

-- OUTPUT Id -- does not work with dapper, replace with second query (SELECT Id FROM...) below
;", staged.Select(DtoHelpers.ToDto));
    var ids = (await conn.QueryAsync<Guid>($"SELECT Id FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE DateStaged=@DateStaged", new { DateStaged = dtstaged })).ToDictionary(id => id);
    return staged.Where(e => ids.ContainsKey(e.Id)).ToList();
  }

  public override async Task Update(List<StagedEntity> staged) {
    await using var conn = await newconn();
    await conn.ExecuteAsync(
        $@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @System, @SystemEntityTypeName, @DatePromoted, @IgnoreReason)) AS se (Id, System, SystemEntityTypeName, DatePromoted, IgnoreReason)
ON 
  T.System = se.System
  AND T.SystemEntityTypeName = se.SystemEntityTypeName
  AND T.Id = se.Id
WHEN MATCHED THEN UPDATE SET DatePromoted = se.DatePromoted, IgnoreReason=se.IgnoreReason;", staged);
  }

  protected override async Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) {
    await using var conn = await newconn();
    var limit = Limit is > 0 and < Int32.MaxValue ? $"TOP {Limit}" : String.Empty;
    var promotedpredicate = incpromoted ? String.Empty : "AND DatePromoted IS NULL";
    var sql = @$"
SELECT {limit} * 
FROM {SCHEMA}.{STAGED_ENTITY_TBL} 
WHERE 
  DateStaged > @after
  AND System = @system
  AND SystemEntityTypeName = @systype  
  AND IgnoreReason IS NULL 
  {promotedpredicate} 
ORDER BY DateStaged
";
    return (await conn.QueryAsync<StagedEntity.Dto>(sql, new { after, system, systype }))
        .Select(e => e.ToBase()).ToList();
  }

  protected override async Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    await using var conn = await newconn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE {col} < @before AND System = @system AND SystemEntityTypeName = @systype", new { before, system, systype });
  }
}

