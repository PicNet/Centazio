using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
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
  CoreEntityChecksum nvarchar(64) NOT NULL, 
  FirstName nvarchar (64) NOT NULL, 
  LastName nvarchar (64) NOT NULL, 
  DateOfBirth date NOT NULL,

  DateCreated datetime2 NULL,
  DateUpdated datetime2 NULL)
END
");
    return this;
  }
  
  public async Task<E> Get<E>(CoreEntityType coretype, CoreEntityId coreid) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raw = await conn.QuerySingleOrDefaultAsync<CoreEntity.Dto>($"SELECT * FROM {coretype} WHERE Id=@Id", new { Id = coreid });
    if (raw is null) throw new Exception($"Core entity [{coretype}({coreid})] not found");
    return (CoreEntity) raw as E ?? throw new Exception();
  }

  
  public async Task<List<E>> Query<E>(CoreEntityType coretype, string query) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raws = await conn.QueryAsync<CoreEntity.Dto>(query);
    return raws.Select(raw => (CoreEntity) raw).Cast<E>().ToList();
  }
  
  public Task<List<E>> Query<E>(CoreEntityType coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityType coretype, List<ICoreEntity> entities) {
    await using var conn = SqlConn.Instance.Conn();
    var ids = entities.Select(e => e.CoreId).ToList();
    var mapping = await conn.QueryAsync<(string Id, string CoreEntityChecksum)>($"SELECT Id, CoreEntityChecksum FROM {coretype} WHERE Id IN (@ids)", new { ids });
    return mapping.ToDictionary(t => new CoreEntityId(t.Id), t => new CoreEntityChecksum(t.CoreEntityChecksum));
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityType coretype, List<Containers.CoreChecksum> entities) {
    var sql = $@"MERGE INTO {coretype} T
USING (VALUES (@Id, @CoreEntityChecksum, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated))
AS c (Id, CoreEntityChecksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated)
ON T.Id = c.Id
WHEN NOT MATCHED THEN
INSERT (Id, CoreEntityChecksum, FirstName, LastName, DateOfBirth, DateCreated)
VALUES (c.Id, c.CoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated)
WHEN MATCHED THEN 
UPDATE SET CoreEntityChecksum=c.CoreEntityChecksum, FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth,
  DateUpdated=c.DateUpdated;";
    
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync(sql, entities.Select(cs => {
      var c = cs.Core.To<CoreEntity>();
      return new { Id = c.CoreId, cs.CoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.DateUpdated };
    }));
    return entities.Select(c => c.Core).ToList();
  }

  public async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreEntity)};");
    }
  }
}