using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.PostgresSql.Tests.Ctl;

public class PostgresSqlCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var connstr = await new PostgresSqlConnection().Init();
    return await new TestingEfCtlRepository(() => new PostgresSqlCtlRepositoryDbContext(connstr,
            nameof(Core.Ctl).ToLower(),
            nameof(SystemState).ToLower(),
            nameof(ObjectState).ToLower(),
            nameof(Map.CoreToSysMap).ToLower()),
        new PostgresSqlDbFieldsHelper()).Initialise();
  }

}