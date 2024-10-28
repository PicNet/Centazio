using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return (ITestingCtlRepository) await new TestingSqlServerCtlRepository(() => new SqlServerCtlContext(connstr)).Initalise();
  }

}

public class SqlServerCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return (ITestingCtlRepository) await new TestingSqlServerCtlRepository(() => new SqlServerCtlContext(connstr)).Initalise();
  }

}


internal class TestingSqlServerCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb) : 
    TestingEFCoreCtlRepository(new SqlServerDbFieldsHelper(), getdb), ITestingCtlRepository {
  
  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var conn = getdb();
    return (await conn.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
}