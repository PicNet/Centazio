﻿using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var connstr = (await SqlConn.GetInstance(false, await TestingFactories.Secrets())).ConnStr;
    return await new TestingEfCtlRepository(() => new SqlServerCtlRepositoryDbContext(connstr), new SqlServerDbFieldsHelper()).Initialise();
  }
}