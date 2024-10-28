using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() => 
      await new TestingSqlServerCtlRepository(await SqlConn.Instance.ConnStr()).Initalise(new SqlServerDbFieldsHelper());

}

public class SqlServerCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() => 
      (ITestingCtlRepository)await new TestingSqlServerCtlRepository(await SqlConn.Instance.ConnStr()).Initalise(new SqlServerDbFieldsHelper());

}


internal class TestingSqlServerCtlRepository(string connstr) : EFCoreCtlRepository(() => new SqlServerCtlContext(connstr)), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var conn = new SqlServerCtlContext(connstr);
    return (await conn.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
}