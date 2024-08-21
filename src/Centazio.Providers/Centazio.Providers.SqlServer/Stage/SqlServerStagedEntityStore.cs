using System.Data;
using Centazio.Core;
using centazio.core.Ctl.Entities;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SQLServer.Stage;

public class SqlServerStagedEntityStore(string connstr, string table, int limit) : AbstractStagedEntityStore(limit) {
  
  protected string ConnStr => connstr;
  
  static SqlServerStagedEntityStore() {
    // SqlMapper.AddTypeHandler(new SystemNameSqlTypeHandler());
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<SystemName>());
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<ObjectName>());
    SqlMapper.AddTypeHandler(new ValidStringSqlTypeHandler<LifecycleStage>());
    SqlMapper.AddTypeHandler(new DateTimeSqlTypeHandler());
    SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
  }

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

  public async Task<SqlServerStagedEntityStore> Initalise() {
    await using var conn = GetConn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
BEGIN
  CREATE TABLE {table} (
    Id uniqueidentifier NOT NULL PRIMARY KEY, 
    SourceSystem nvarchar (64) NOT NULL, 
    Object nvarchar (64) NOT NULL, 
    DateStaged datetime2 NOT NULL, 
    Data nvarchar (max) NOT NULL,
    DatePromoted datetime2 NULL,
    Ignore nvarchar (256) NULL)

ALTER TABLE dbo.{table} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE);
CREATE INDEX ix_{table}_source_obj_staged ON dbo.{table} (SourceSystem, Object, DateStaged);
END
");
    return this;
  }

  protected override async Task<StagedEntity> SaveImpl(StagedEntity staged) => await DoSqlUpsert(SqlServerStagedEntity.FromStagedEntity(staged));
  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> staged) => await DoSqlUpsert(staged.Select(SqlServerStagedEntity.FromStagedEntity));

  private async Task<T> DoSqlUpsert<T>(T staged) {
    await using var conn = GetConn();
    await conn.ExecuteAsync(
        $@"MERGE INTO {table}
USING (VALUES (@Id, @SourceSystem, @Object, @DateStaged, @Data, @DatePromoted, @Ignore))
  AS se (Id, SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore)
ON {table}.Id = se.Id
WHEN MATCHED THEN
 UPDATE SET DatePromoted = se.DatePromoted, Ignore=se.Ignore
WHEN NOT MATCHED THEN
 INSERT (Id, SourceSystem, Object, DateStaged, Data)
 VALUES (se.Id, se.SourceSystem, se.Object, se.DateStaged, se.Data);
", staged);
    return staged;
  }

  public override Task Update(StagedEntity staged) => SaveImpl(staged);
  public override Task Update(IEnumerable<StagedEntity> staged) => SaveImpl(staged);

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    await using var conn = GetConn();
    var limit = Limit > 0 ? $" TOP {Limit}" : "";
    return await conn.QueryAsync<SqlServerStagedEntity>($"SELECT{limit} * FROM {table} WHERE DateStaged > @since AND Ignore IS NULL ORDER BY DateStaged", new { since });
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    await using var conn = GetConn();
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {table} WHERE {col} < @before AND SourceSystem = @source AND Object = @obj", new { before, source, obj });
  }
  
  private SqlConnection GetConn() => new(connstr);
}

public class ValidStringSqlTypeHandler<T> : SqlMapper.TypeHandler<T> where T : ValidString {
  public override void SetValue(IDbDataParameter parameter, T? value) => parameter.Value = value?.Value ?? throw new Exception($"{nameof(value)} must ne non-empty");
  public override T Parse(object? value) {
    ArgumentException.ThrowIfNullOrWhiteSpace((string?) value);
    return (T?) Activator.CreateInstance(typeof(T), (string?) value) ?? throw new Exception();
  }

}

public class DateTimeSqlTypeHandler : SqlMapper.TypeHandler<DateTime> {
  public override void SetValue(IDbDataParameter parameter, DateTime value) { parameter.Value = value; }
  public override DateTime Parse(object value) { return DateTime.SpecifyKind((DateTime) value, DateTimeKind.Utc); }
}

