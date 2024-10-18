using Centazio.Core.Ctl.Entities;
using Centazio.Providers.Sqlite.CoreToSystemMapping;
using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;
using Dapper;

namespace Centazio.Providers.Sqlite.Tests.CoreToSystemMapping;

public class SqliteCoreToSystemMapStoreTests : AbstractCoreToSystemMapStoreTests {
  protected override async Task<ITestingCoreToSystemMapStore> GetStore() => (ITestingCoreToSystemMapStore) await new TestingSqliteCoreToSystemMapStore().Initalise();
}

public class TestingSqliteCoreToSystemMapStore() : SqliteCoreToSystemMapStore(SqliteConn.Instance.Conn), ITestingCoreToSystemMapStore {
  
  public async Task<List<Map.CoreToSystemMap>> GetAll() {
    await using var conn = SqliteConn.Instance.Conn();
    var dtos = await conn.QueryAsync<Map.CoreToSystemMap.Dto>($"SELECT * FROM [{MAPPING_TBL}]");
    return dtos.Select(dto => dto.ToBase()).ToList();
  }
 
  public override async ValueTask DisposeAsync() {
    await using var conn = SqliteConn.Instance.Conn();
    // todo: move `await using var conn = SqliteConn.Instance.Conn()` to Db.cs
    await Db.Exec(conn, $"DROP TABLE [{MAPPING_TBL}]");
  }
}