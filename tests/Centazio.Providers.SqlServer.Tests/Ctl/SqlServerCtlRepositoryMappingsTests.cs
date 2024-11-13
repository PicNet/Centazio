using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var connstr = (await SqlConn.GetInstance(false)).ConnStr;
    return (ITestingCtlRepository) await new TestingEfCtlRepository(() => new SqlServerCtlRepositoryDbContext(connstr), new SqlServerDbFieldsHelper()).Initialise();
  }

}