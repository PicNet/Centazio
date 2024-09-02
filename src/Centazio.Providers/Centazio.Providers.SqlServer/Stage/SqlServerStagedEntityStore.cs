﻿using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SQLServer.Stage;

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
    Ignore nvarchar (256) NULL)

ALTER TABLE {SCHEMA}.{STAGED_ENTITY_TBL} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
CREATE INDEX ix_{STAGED_ENTITY_TBL}_source_obj_staged ON {SCHEMA}.{STAGED_ENTITY_TBL} (SourceSystem, Object, DateStaged);
END
");
    return this;
  }

  protected override async Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged) {
    await using var conn = newconn();
    var sqlstaged = staged.Select(SqlServerStagedEntity.FromStagedEntity).ToList();
    var checksums = sqlstaged.Select(e => e.Checksum).ToList();
    var duplicates = (await conn.QueryAsync<string>($"SELECT Checksum FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE Checksum IN ({String.Join(',', checksums.Select(cs => $"'{cs}'"))})")).ToDictionary(cs => cs, _ => true);
    var local = new Dictionary<string, bool>();
    var tostage = sqlstaged.Where(e => !duplicates.ContainsKey(e.Checksum) && local.TryAdd(e.Checksum, true)).ToList();
    
    await conn.ExecuteAsync($@"INSERT INTO {SCHEMA}.{STAGED_ENTITY_TBL} (Id, SourceSystem, Object, DateStaged, Data, Checksum)
        VALUES (@Id, @SourceSystem, @Object, @DateStaged, @Data, @Checksum)", tostage);
    
    return tostage;
  }

  public override async Task Update(IEnumerable<StagedEntity> staged) {
    await using var conn = newconn();
    await conn.ExecuteAsync(
        $@"MERGE INTO {SCHEMA}.{STAGED_ENTITY_TBL}
USING (VALUES (@Id, @DatePromoted, @Ignore)) AS se (Id, DatePromoted, Ignore)
ON {SCHEMA}.{STAGED_ENTITY_TBL}.Id = se.Id
WHEN MATCHED THEN UPDATE SET DatePromoted = se.DatePromoted, Ignore=se.Ignore;", staged);
  }

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj) {
    await using var conn = newconn();
    var limit = Limit > 0 ? $" TOP {Limit}" : "";
    return await conn.QueryAsync<SqlServerStagedEntity>($"SELECT{limit} * FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE DateStaged > @since AND Ignore IS NULL ORDER BY DateStaged", new { since = after });
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    await using var conn = newconn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {SCHEMA}.{STAGED_ENTITY_TBL} WHERE {col} < @before AND SourceSystem = @source AND Object = @obj", new { before, source, obj });
  }
}

