using System.Linq.Expressions;
using Centazio.Core.CoreRepo;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

internal class TestingSqlServerCoreStorageRepository : ICoreStorageRepository {

  public async Task<ICoreStorageRepository> Initalise() {
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{nameof(CoreCustomer)}' AND xtype='U')
BEGIN
CREATE TABLE {nameof(CoreCustomer)} (
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
  
  public async Task<C> Get<C>(string id) where C : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raw = await conn.QuerySingleOrDefaultAsync<CoreCustomerRaw>($"SELECT * FROM {typeof(C).Name} WHERE Id=@Id", new { Id = id });
    if (raw == null) throw new Exception($"Core entity [{typeof(C).Name}#{id}] not found");
    return (CoreCustomer) raw as C ?? throw new Exception();
  }

  public async Task<IEnumerable<C>> Query<C>(string query) where C : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raws = await conn.QueryAsync<CoreCustomerRaw>(query);
    return raws.Select(raw => (CoreCustomer) raw).Cast<C>();
  }
  
  public Task<IEnumerable<C>> Query<C>(Expression<Func<C, bool>> predicate) where C : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<string, string>> GetChecksums<C>(List<C> entities) where C : ICoreEntity {
    if (typeof(C) != typeof(CoreCustomer)) throw new NotSupportedException(typeof(C).Name);

    await using var conn = SqlConn.Instance.Conn();
    var ids = entities.Select(e => e.Id).ToList();
    var mapping = await conn.QueryAsync<(string Id, string Checksum)>($"SELECT Id, Checksum FROM {nameof(CoreCustomer)} WHERE Id IN (@ids)", new { ids });
    return mapping.ToDictionary(t => t.Id, t => t.Checksum);
  }

  public async Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    if (typeof(T) != typeof(CoreCustomer)) throw new NotSupportedException(typeof(T).Name);
    
    var sql = $@"MERGE INTO {nameof(CoreCustomer)} T
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
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreCustomer)};");
    }
  }
}