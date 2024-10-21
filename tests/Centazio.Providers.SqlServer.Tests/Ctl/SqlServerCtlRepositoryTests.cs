using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() => await new TestingSqlServerCtlRepository().Initalise();
}

public class SqlServerCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() => (ITestingCtlRepository) await new TestingSqlServerCtlRepository().Initalise();
}


internal class TestingSqlServerCtlRepository() : SqlServerCtlRepository(async () => await SqlConn.Instance.Conn()), ITestingCtlRepository {
  public override async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = await SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SCHEMA}.{SYSTEM_STATE_TBL}; DROP TABLE IF EXISTS {SCHEMA}.{MAPPING_TBL};");
    }
    await base.DisposeAsync();
  }

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var conn = await SqlConn.Instance.Conn();
    var dtos = await conn.QueryAsync<Map.CoreToSysMap.Dto>($"SELECT * FROM [{SCHEMA}].[{MAPPING_TBL}]");
    return dtos.Select(dto => dto.ToBase()).ToList();
  }

}