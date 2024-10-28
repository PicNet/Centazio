using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

// todo: remove and replace with EFCoreStorageRepository
internal class TestingSqlServerCoreStorageRepository : ICoreStorageWithQuery {
  
  public async Task<ICoreStorageWithQuery> Initalise(IDbFieldsHelper dbf) {
    await using var conn = await SqlConn.Instance.Conn();
    var fields = dbf.GetDbFields<CoreEntity>();
    fields.Add(new (nameof(CoreEntityChecksum), typeof(string), ChecksumValue.MAX_LENGTH.ToString(), true));
    await conn.ExecuteAsync(dbf.GenerateCreateTableScript("dbo", nameof(CoreEntity), fields, [nameof(CoreEntity.CoreId)]));
    return this;
  }
  
  public async Task<List<ICoreEntity>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    await using var conn = await SqlConn.Instance.Conn();
    var dtos = await conn.QueryAsync<CoreEntity.Dto>($"SELECT * FROM [{coretype}] WHERE [{nameof(ICoreEntity.System)}] != @ExcludeSystem [{nameof(ICoreEntity.DateUpdated)}] > @After", new { After=after, ExcludeSystem=exclude });
    return dtos.Select(dto => (ICoreEntity) dto.ToBase()).ToList();
  }
  
  public async Task<List<ICoreEntity>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = await SqlConn.Instance.Conn();
    var dtos = (await conn.QueryAsync<CoreEntity.Dto>($"SELECT * FROM {coretype} WHERE CoreId IN @CoreIds", new { CoreIds=coreids.Select(id => id.Value) })).ToList();
    if (dtos.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return dtos.Select(dto => (ICoreEntity) dto.ToBase()).ToList();
  }

  
  public async Task<List<E>> Query<E>(CoreEntityTypeName coretype, string query) where E : class, ICoreEntity {
    await using var conn = await SqlConn.Instance.Conn();
    var dtos = await conn.QueryAsync<CoreEntity.Dto>(query);
    return dtos.Select(dto => dto.ToBase() as E ?? throw new Exception()).ToList();
  }
  
  public Task<List<E>> Query<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = await SqlConn.Instance.Conn();
    var mapping = await conn.QueryAsync<(string CoreId, string CoreEntityChecksum)>($"SELECT CoreId, CoreEntityChecksum FROM {coretype} WHERE CoreId IN (@coreids)", new { coreids });
    return mapping.ToDictionary(t => new CoreEntityId(t.CoreId), t => new CoreEntityChecksum(t.CoreEntityChecksum));
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var sql = $@"MERGE INTO {coretype} T
USING (VALUES (@CoreId, @CoreEntityChecksum, @FirstName, @LastName, @DateOfBirth, @SystemId, @System, @LastUpdateSystem, @DateCreated, @DateUpdated))
AS c (CoreId, CoreEntityChecksum, FirstName, LastName, DateOfBirth, SystemId, System, LastUpdateSystem, DateCreated, DateUpdated)
ON T.CoreId = c.CoreId
WHEN NOT MATCHED THEN
  INSERT (CoreId, CoreEntityChecksum, FirstName, LastName, DateOfBirth, SystemId, System, LastUpdateSystem, DateCreated, DateUpdated)
  VALUES (c.CoreId, c.CoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.SystemId, c.System, c.LastUpdateSystem, c.DateCreated, c.DateUpdated)
WHEN MATCHED THEN 
  UPDATE SET CoreEntityChecksum=c.CoreEntityChecksum, FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth, LastUpdateSystem=c.LastUpdateSystem, DateUpdated=c.DateUpdated;";
    
    await using var conn = await SqlConn.Instance.Conn();
    await conn.ExecuteAsync(sql, entities.Select(cs => {
      var c = cs.UpdatedCoreEntity.To<CoreEntity>();
      return new { c.CoreId, CoreEntityChecksum = cs.UpdatedCoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.SystemId, c.System, c.LastUpdateSystem, c.DateCreated, c.DateUpdated };
    }));
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }

  public async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = await SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreEntity)};");
    }
  }
}