using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityStore(Func<SqlConnection> newconn, int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityStore(limit, checksum) {

  internal static readonly string SCHEMA = nameof(Core.Ctl).ToLower();
  internal const string STAGED_ENTITY_TBL = nameof(StagedEntity);

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

  public async Task<SqlServerStagedEntityStore> Initalise() {
    await using var conn = newconn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{SCHEMA}')
  EXEC('CREATE SCHEMA [{SCHEMA}] AUTHORIZATION [dbo]');

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{STAGED_ENTITY_TBL}' AND xtype='U')
BEGIN
  CREATE TABLE {SCHEMA}.{STAGED_ENTITY_TBL} (
    Id uniqueidentifier NOT NULL PRIMARY KEY, 
    System nvarchar (64) NOT NULL, 
    SystemEntityType nvarchar (64) NOT NULL, 
    DateStaged datetime2 NOT NULL, 
    Data nvarchar (max) NOT NULL,
    StagedEntityChecksum nvarchar (64) NOT NULL,
    DatePromoted datetime2 NULL,
    IgnoreReason nvarchar (256) NULL)

ALTER TABLE {SCHEMA}.{STAGED_ENTITY_TBL} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
CREATE INDEX ix_{STAGED_ENTITY_TBL}_source_obj_staged ON {SCHEMA}.{STAGED_ENTITY_TBL} (System, SystemEntityType, DateStaged);
END
");
    return this;
  }

  protected override async Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    // all staged entities will have the same DateStaged so just use first as the id of this bulk insert
    var dtstaged = staged.First().DateStaged;
    await using var conn = newconn();
    await conn.ExecuteAsync($@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @System, @SystemEntityType, @DateStaged, @Data, @StagedEntityChecksum))
  AS se (Id, System, SystemEntityType, DateStaged, Data, StagedEntityChecksum)
ON 
  T.System = se.System
  AND T.SystemEntityType = se.SystemEntityType
  AND T.StagedEntityChecksum = se.StagedEntityChecksum
WHEN NOT MATCHED THEN
 INSERT (Id, System, SystemEntityType, DateStaged, Data, StagedEntityChecksum)
 VALUES (se.Id, se.System, se.SystemEntityType, se.DateStaged, se.Data, se.StagedEntityChecksum)
-- OUTPUT Id -- does not work with dapper, replace with second query (SELECT Id FROM...) below
;", staged.Select(e => (StagedEntity.Dto) e).ToList());
    var ids = (await conn.QueryAsync<Guid>($"SELECT Id FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE DateStaged=@DateStaged", new { DateStaged = dtstaged })).ToDictionary(id => id);
    return staged.Where(e => ids.ContainsKey(e.Id)).ToList();
  }

  public override async Task Update(List<StagedEntity> staged) {
    await using var conn = newconn();
    await conn.ExecuteAsync(
        $@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @System, @SystemEntityType, @DatePromoted, @IgnoreReason)) AS se (Id, System, SystemEntityType, DatePromoted, IgnoreReason)
ON 
  T.System = se.System
  AND T.SystemEntityType = se.SystemEntityType
  AND T.Id = se.Id
WHEN MATCHED THEN UPDATE SET DatePromoted = se.DatePromoted, IgnoreReason=se.IgnoreReason;", staged);
  }

  protected override async Task<List<StagedEntity>> GetImpl(DateTime after, SystemName system, SystemEntityType systype, bool incpromoted) {
    await using var conn = newconn();
    var limit = Limit > 0 ? $"TOP {Limit}" : String.Empty;
    var promotedpredicate = incpromoted ? String.Empty : "AND DatePromoted IS NULL";
    var sql = @$"
SELECT {limit} * 
FROM {SCHEMA}.{STAGED_ENTITY_TBL} 
WHERE 
  DateStaged > @after
  AND System = @system
  AND SystemEntityType = @systype  
  AND IgnoreReason IS NULL 
  {promotedpredicate} 
ORDER BY DateStaged
";
    return (await conn.QueryAsync<StagedEntity.Dto>(sql, new { after, system, systype }))
        .Select(e => (StagedEntity) e).ToList();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName system, SystemEntityType systype, bool promoted) {
    await using var conn = newconn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE {col} < @before AND System = @system AND SystemEntityType = @systype", new { before, system, systype });
  }
}

