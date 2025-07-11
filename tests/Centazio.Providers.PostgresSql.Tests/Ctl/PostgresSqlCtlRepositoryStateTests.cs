using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.PostgresSql.Tests.Ctl;

public class PostgresSqlCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var settings = (await F.Settings()).CtlRepository with { ConnectionString = await new PostgresSqlConnection().Init() };
    return await new TestingEfCtlRepository(() => new PostgresSqlCtlRepositoryDbContext(settings)).Initialise();
  }

}