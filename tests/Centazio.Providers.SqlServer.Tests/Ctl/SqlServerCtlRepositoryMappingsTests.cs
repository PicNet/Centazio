using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var connstr = (await SqlConn.GetInstance(false, await TestingFactories.Secrets())).ConnStr;
    var settings = (await TestingFactories.Settings()).CtlRepository with { ConnectionString = connstr };
    return (ITestingCtlRepository) await new TestingEfCtlRepository(() => new SqlServerCtlRepositoryDbContext(settings), new SqlServerDbFieldsHelper()).Initialise();
  }

}