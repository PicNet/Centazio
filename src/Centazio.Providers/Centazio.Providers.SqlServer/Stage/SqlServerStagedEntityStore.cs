using System.Data;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SQLServer.Stage;

public record SqlServerStagedEntityStoreConfiguration(string ConnectionString, string Table, int Limit);

public class SqlServerStagedEntityStore(SqlServerStagedEntityStoreConfiguration config) : AbstractStagedEntityStore {
  
  static SqlServerStagedEntityStore() {
    SqlMapper.AddTypeHandler(new StringValueSqlTypeHandler<SystemName>());
    SqlMapper.AddTypeHandler(new StringValueSqlTypeHandler<ObjectName>());
    SqlMapper.AddTypeHandler(new StringValueSqlTypeHandler<LifecycleStage>());
    SqlMapper.AddTypeHandler(new DateTimeSqlTypeHandler());
    SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);
  }

  public override ValueTask DisposeAsync() { return ValueTask.CompletedTask; }

  public async Task<SqlServerStagedEntityStore> Initalise() {
    await using var conn = new SqlConnection(config.ConnectionString);
    await conn.ExecuteAsync($@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{config.Table}' AND xtype='U')
    CREATE TABLE {config.Table} (
      Id uniqueidentifier NOT NULL PRIMARY KEY, 
      SourceSystem nvarchar (64) NOT NULL, 
      Object nvarchar (64) NOT NULL, 
      DateStaged datetime2 NOT NULL, 
      Data nvarchar (max) NOT NULL,
      DatePromoted datetime2 NULL,
      Ignore nvarchar (256) NULL
    )");
    return this;
  }

  protected override async Task<StagedEntity> SaveImpl(StagedEntity se) {
    var sqlse = SqlServerStagedEntity.FromStagedEntity(se);
    await using var conn = new SqlConnection(config.ConnectionString);
    
    await conn.ExecuteAsync(
$@"IF ((SELECT COUNT(*) FROM {config.Table} where Id=@Id)=1)
  BEGIN
    UPDATE {config.Table}
      SET
        DatePromoted = @DatePromoted,
        Ignore = @Ignore
      WHERE ID = @Id;
  END
ELSE
  BEGIN
    INSERT INTO {config.Table} (Id, SourceSystem, Object, DateStaged, Data)
      VALUES (@Id, @SourceSystem, @Object, @DateStaged, @Data)
  END", sqlse);
    return sqlse;
  }

  protected override async Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> ses) {
    var lst = ses.Select(SqlServerStagedEntity.FromStagedEntity).ToList();
    await using var conn = new SqlConnection(config.ConnectionString);
    await conn.ExecuteAsync(
$@"MERGE INTO {config.Table}
USING (VALUES (@Id, @SourceSystem, @Object, @DateStaged, @Data, @DatePromoted, @Ignore))
  AS se (Id, SourceSystem, Object, DateStaged, Data, DatePromoted, Ignore)
ON {config.Table}.Id = se.Id
WHEN MATCHED THEN
 UPDATE SET DatePromoted = se.DatePromoted, Ignore=se.Ignore
WHEN NOT MATCHED THEN
 INSERT (Id, SourceSystem, Object, DateStaged, Data)
 VALUES (se.Id, se.SourceSystem, se.Object, se.DateStaged, se.Data);
", lst);
    return lst.AsEnumerable();
  }

  public override Task Update(StagedEntity staged) => SaveImpl(staged);
  public override Task Update(IEnumerable<StagedEntity> staged) => SaveImpl(staged);

  protected override async Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) {
    await using var conn = new SqlConnection(config.ConnectionString);
    var limit = config.Limit > 0 ? $" TOP {config.Limit}" : "";
    return await conn.QueryAsync<SqlServerStagedEntity>($"SELECT{limit} * FROM {config.Table} WHERE DateStaged > @since AND Ignore IS NULL ORDER BY DateStaged", new { since });
  }

  protected override async Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    await using var conn = new SqlConnection(config.ConnectionString);
    var col = promoted ? nameof(StagedEntity.DatePromoted) : nameof(StagedEntity.DateStaged);
    await conn.ExecuteAsync($"DELETE FROM {config.Table} WHERE {col} < @before AND SourceSystem = @source AND Object = @obj", new { before, source, obj });
  }
}

public class StringValueSqlTypeHandler<T> : SqlMapper.TypeHandler<T> where T : IStringValue, new() {
  public override void SetValue(IDbDataParameter parameter, T? value) { parameter.Value = value?.Value; }
  public override T? Parse(object? value) => value == default ? default : new T { Value = (string) value };
}

public class DateTimeSqlTypeHandler : SqlMapper.TypeHandler<DateTime> {
  public override void SetValue(IDbDataParameter parameter, DateTime value) { parameter.Value = value; }
  public override DateTime Parse(object value) { return DateTime.SpecifyKind((DateTime) value, DateTimeKind.Utc); }
}

