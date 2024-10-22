using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Centazio.Test.Lib.CoreStorage;
using Dapper;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

internal class TestingSqliteCoreStorageRepository : ICoreStorageWithQuery {

  public async Task<ICoreStorageWithQuery> Initalise() {
    await using var conn = SqliteConn.Instance.Conn();
    var dbf = new DbFieldsHelper();
    var fields = dbf.GetDbFields<CoreEntity>();
    fields.Add(new (nameof(CoreEntityChecksum), typeof(string), ChecksumValue.MAX_LENGTH.ToString(), true));
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(nameof(CoreEntity), fields, [nameof(CoreEntity.CoreId)]));
    return this;
  }
  
  public async Task<List<ICoreEntity>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    await using var conn = SqliteConn.Instance.Conn();
    var dtos = await Db.Query<CoreEntity.Dto>(conn, $"SELECT * FROM [{coretype}] WHERE [{nameof(ICoreEntity.System)}] != @ExcludeSystem [{nameof(ICoreEntity.DateUpdated)}] > @After", new { After=after, ExcludeSystem=exclude });
    return dtos.Select(dto => (ICoreEntity) dto.ToBase()).ToList();
  }

  public async Task<List<ICoreEntity>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = SqliteConn.Instance.Conn();
    var dtos = await Db.Query<CoreEntity.Dto>(conn, $"SELECT * FROM {coretype} WHERE CoreId IN @CoreIds", new { CoreIds=coreids.Select(id => id.Value) });
    if (dtos.Count != coreids.Count) throw new Exception($"Some core entities could not be found");
    return dtos.Select(dto => (ICoreEntity) dto.ToBase()).ToList();
  }

  public async Task<List<E>> Query<E>(CoreEntityTypeName coretype, string query) where E : class, ICoreEntity {
    await using var conn = SqliteConn.Instance.Conn();
    var dtos = await conn.QueryAsync<CoreEntity.Dto>(query);
    return dtos.Select(dto => dto.ToBase() as E ?? throw new Exception()).ToList();
  }
  
  public Task<List<E>> Query<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity => throw new NotSupportedException();
  
  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var conn = SqliteConn.Instance.Conn();
    var mapping = await conn.QueryAsync<(string CoreId, string CoreEntityChecksum)>($"SELECT CoreId, CoreEntityChecksum FROM {coretype} WHERE CoreId IN (@coreids)", new { coreids });
    return mapping.ToDictionary(t => new CoreEntityId(t.CoreId), t => new CoreEntityChecksum(t.CoreEntityChecksum));
  }

  public async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var sql = $@"
INSERT INTO {coretype} (System, SystemId, CoreId, CoreEntityChecksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated, LastUpdateSystem)
  VALUES (@System, @SystemId, @CoreId, @CoreEntityChecksum, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated, @LastUpdateSystem)
ON CONFLICT DO 
  UPDATE SET CoreEntityChecksum=@CoreEntityChecksum, FirstName=@FirstName, LastName=@LastName, DateOfBirth=@DateOfBirth, DateUpdated=@DateUpdated, LastUpdateSystem=@LastUpdateSystem
  WHERE CoreId=@CoreId;";
    
    await using var conn = SqliteConn.Instance.Conn();
    await Db.Exec(conn, sql, entities.Select(cs => {
      var c = cs.UpdatedCoreEntity.To<CoreEntity>();
      return new { c.System, c.SystemId, c.CoreId, CoreEntityChecksum = cs.UpdatedCoreEntityChecksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.DateUpdated, c.LastUpdateSystem };
    }));
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }

  public async ValueTask DisposeAsync() {
    await using var conn = SqliteConn.Instance.Conn();
    await Db.Exec(conn, $"DROP TABLE IF EXISTS {nameof(CoreEntity)}");
  }
}