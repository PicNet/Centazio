using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return await new TestingEfCtlRepository(() => new SqlServerCtlContext(connstr), new SqlServerDbFieldsHelper()).Initalise();
  }
}