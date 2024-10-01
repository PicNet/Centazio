using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.CoreStorage;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

internal class TestingSqlServerCoreStorageRepository : ICoreStorageRepository {

  public async Task<ICoreStorageRepository> Initalise() {
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{nameof(CoreEntity)}' AND xtype='U')
BEGIN
CREATE TABLE {nameof(CoreEntity)} (
  Id nvarchar(64) NOT NULL PRIMARY KEY,
  Checksum nvarchar(64) NOT NULL, 
  FirstName nvarchar (64) NOT NULL, 
  LastName nvarchar (64) NOT NULL, 
  DateOfBirth date NOT NULL,

  DateCreated datetime2 NULL,
  DateUpdated datetime2 NULL,
  SourceSystemDateUpdated datetime2 NULL)
END
");
    return this;
  }
  
  public async Task<E> Get<E>(CoreEntityType obj, string id) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raw = await conn.QuerySingleOrDefaultAsync<CoreEntity.Dto>($"SELECT * FROM {obj} WHERE Id=@Id", new { Id = id });
    if (raw is null) throw new Exception($"Core entity [{obj}#{id}] not found");
    return (CoreEntity) raw as E ?? throw new Exception();
  }

  
  public async Task<List<E>> Query<E>(CoreEntityType obj, string query) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raws = await conn.QueryAsync<CoreEntity.Dto>(query);
    return raws.Select(raw => (CoreEntity) raw).Cast<E>().ToList();
  }
  
  public Task<List<E>> Query<E>(CoreEntityType obj, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    await using var conn = SqlConn.Instance.Conn();
    var ids = entities.Select(e => e.Id).ToList();
    var mapping = await conn.QueryAsync<(string Id, string Checksum)>($"SELECT Id, Checksum FROM {obj} WHERE Id IN (@ids)", new { ids });
    return mapping.ToDictionary(t => t.Id, t => t.Checksum);
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<ICoreEntity> entities) {
    var sql = $@"MERGE INTO {obj} T
USING (VALUES (@Id, @Checksum, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated, @SourceSystemDateUpdated))
AS c (Id, Checksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated, SourceSystemDateUpdated)
ON T.Id = c.Id
WHEN NOT MATCHED THEN
INSERT (Id, Checksum, FirstName, LastName, DateOfBirth, DateCreated, SourceSystemDateUpdated)
VALUES (c.Id, c.Checksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.SourceSystemDateUpdated)
WHEN MATCHED THEN 
UPDATE SET Checksum=c.Checksum, FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth,
  DateUpdated=c.DateUpdated, SourceSystemDateUpdated=c.SourceSystemDateUpdated;";
    
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync(sql, entities);
    return entities;
  }

  public async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreEntity)};");
    }
  }
}