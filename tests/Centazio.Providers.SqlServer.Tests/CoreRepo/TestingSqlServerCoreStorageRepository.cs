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
  CoreId nvarchar(64) NOT NULL PRIMARY KEY,
  CoreEntityChecksum nvarchar(64) NOT NULL,
  FirstName nvarchar (64) NOT NULL, 
  LastName nvarchar (64) NOT NULL, 
  DateOfBirth date NOT NULL,
  DateCreated datetime2 NOT NULL,
  DateUpdated datetime2 NOT NULL)
END
");
    return this;
  }
  
  public async Task<E> Get<E>(CoreEntityTypeName coretype, CoreEntityId coreid) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var dto = await conn.QuerySingleOrDefaultAsync<CoreEntity.Dto>($"SELECT * FROM {coretype} WHERE CoreId=@coreid", new { coreid });
    if (dto is null) throw new Exception($"Core entity [{coretype}({coreid})] not found");
    return dto.ToCoreEntity() as E ?? throw new Exception();
  }

  
  public async Task<List<E>> Query<E>(CoreEntityTypeName coretype, string query) where E : class, ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var dtos = await conn.QueryAsync<CoreEntity.Dto>(query);
    return dtos.Select(dto => dto.ToCoreEntity()).Cast<E>().ToList();
  }
  
  public Task<List<E>> Query<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = SqlConn.Instance.Conn();
    var mapping = await conn.QueryAsync<(string CoreId, string CoreEntityChecksum)>($"SELECT CoreId, CoreEntityChecksum FROM {coretype} WHERE CoreId IN (@coreids)", new { coreids });
    return mapping.ToDictionary(t => new CoreEntityId(t.CoreId), t => new CoreEntityChecksum(t.CoreEntityChecksum));
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var sql = $@"MERGE INTO {coretype} T
USING (VALUES (@CoreId, @CoreEntityChecksum, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated))
AS c (CoreId, CoreEntityChecksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated)
ON T.CoreId = c.CoreId
WHEN NOT MATCHED THEN
INSERT (CoreId, CoreEntityChecksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated)
VALUES (c.CoreId, c.CoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.DateUpdated)
WHEN MATCHED THEN 
UPDATE SET CoreEntityChecksum=c.CoreEntityChecksum, FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth, DateUpdated=c.DateUpdated;";
    
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync(sql, entities.Select(cs => {
      var c = cs.UpdatedCoreEntity.To<CoreEntity>();
      return new { c.CoreId, cs.UpdatedCoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.DateUpdated };
    }));
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }

  public async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreEntity)};");
    }
  }
}