using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityStore(Func<SqlConnection> newconn, int limit, Func<string, string> checksum) : AbstractStagedEntityStore(limit, checksum) {

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
    SourceSystem nvarchar (64) NOT NULL, 
    Object nvarchar (64) NOT NULL, 
    DateStaged datetime2 NOT NULL, 
    Data nvarchar (max) NOT NULL,
    Checksum nvarchar (64) NOT NULL,
    DatePromoted datetime2 NULL,
    IgnoreReason nvarchar (256) NULL)

ALTER TABLE {SCHEMA}.{STAGED_ENTITY_TBL} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
CREATE INDEX ix_{STAGED_ENTITY_TBL}_source_obj_staged ON {SCHEMA}.{STAGED_ENTITY_TBL} (SourceSystem, Object, DateStaged);
END
");
    return this;
  }

  protected override async Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    // all staged entities will have the same DateStaged so just use first as the id of this bulk insert
    var dtstaged = staged.First().DateStaged;
    await using var conn = newconn();
    await conn.ExecuteAsync($@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @SourceSystem, @Object, @DateStaged, @Data, @Checksum))
  AS se (Id, SourceSystem, Object, DateStaged, Data, Checksum)
ON 
  T.SourceSystem = se.SourceSystem
  AND T.Object = se.Object
  AND T.Checksum = se.Checksum
WHEN NOT MATCHED THEN
 INSERT (Id, SourceSystem, Object, DateStaged, Data, Checksum)
 VALUES (se.Id, se.SourceSystem, se.Object, se.DateStaged, se.Data, se.Checksum)
-- OUTPUT Id -- does not work with dapper, replace with second query (SELECT Id FROM...) below
;", staged.Select(e => (StagedEntity.Dto) e).ToList());
    var ids = (await conn.QueryAsync<Guid>($"SELECT Id FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE DateStaged=@DateStaged", new { DateStaged = dtstaged })).ToDictionary(id => id);
    return staged.Where(e => ids.ContainsKey(e.Id)).ToList();
  }

  public override async Task Update(List<StagedEntity> staged) {
    await using var conn = newconn();
    await conn.ExecuteAsync(
        $@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL} T
USING (VALUES (@Id, @SourceSystem, @Object, @DatePromoted, @IgnoreReason)) AS se (Id, SourceSystem, Object, DatePromoted, IgnoreReason)
ON 
  T.SourceSystem = se.SourceSystem
  AND T.Object = se.Object
  AND T.Id = se.Id
WHEN MATCHED THEN UPDATE SET DatePromoted = se.DatePromoted, IgnoreReason=se.IgnoreReason;", staged);
  }

  protected override async Task<List<StagedEntity>> GetImpl(DateTime after, SystemName source, ExternalEntityType obj, bool incpromoted) {
    await using var conn = newconn();
    var limit = Limit > 0 ? $"TOP {Limit}" : "";
    var promotedpredicate = incpromoted ? "" : "AND DatePromoted IS NULL";
    var sql = @$"
SELECT {limit} * 
FROM {SCHEMA}.{STAGED_ENTITY_TBL} 
WHERE 
  DateStaged > @after
  AND SourceSystem = @source
  AND Object = @obj  
  AND IgnoreReason IS NULL 
  {promotedpredicate} 
ORDER BY DateStaged
";
    return (await conn.QueryAsync<StagedEntity.Dto>(sql, new { after, source, obj }))
        .Select(e => (StagedEntity) e).ToList();
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ExternalEntityType obj, bool promoted) {
    await using var conn = newconn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE {col} < @before AND SourceSystem = @source AND Object = @obj", new { before, source, obj });
  }
}

