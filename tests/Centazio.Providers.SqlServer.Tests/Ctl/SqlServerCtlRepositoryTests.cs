using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return await new TestingEFCoreCtlRepository(() => new SqlServerCtlContext(connstr), new SqlServerDbFieldsHelper()).Initalise();
  }
}

public class SqlServerCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return (ITestingCtlRepository) await new TestingEFCoreCtlRepository(() => new SqlServerCtlContext(connstr), new SqlServerDbFieldsHelper()).Initalise();
  }

}