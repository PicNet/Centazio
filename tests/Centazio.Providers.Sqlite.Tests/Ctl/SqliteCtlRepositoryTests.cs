using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Dapper;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() => await new TestingSqliteCtlRepository().Initalise();
}

public class SqliteCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() => (ITestingCtlRepository)await new TestingSqliteCtlRepository().Initalise();
}

internal class TestingSqliteCtlRepository() : SqliteCtlRepository(SqliteConn.Instance.Conn), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var conn = SqliteConn.Instance.Conn();
    var dtos = await conn.QueryAsync<Map.CoreToSysMap.Dto>($"SELECT * FROM [{MAPPING_TBL}]");
    return dtos.Select(dto => dto.ToBase()).ToList();
  }

  public override async ValueTask DisposeAsync() {
    await using var conn = SqliteConn.Instance.Conn();
    await Db.Exec(conn, $"DROP TABLE IF EXISTS {OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SYSTEM_STATE_TBL}; DROP TABLE IF EXISTS {MAPPING_TBL};");
  }

}