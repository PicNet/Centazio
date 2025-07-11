using Centazio.Providers.EF.Tests;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.PostgresSql.Tests.Ctl;

public class PostgresSqlCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var settings = (await F.Settings()).CtlRepository with { ConnectionString = await new PostgresSqlConnection().Init() };
    return (ITestingCtlRepository) await new TestingEfCtlRepository(() => new PostgresSqlCtlRepositoryDbContext(settings)).Initialise();
  }
}