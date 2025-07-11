using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var settings = (await F.Settings()).CtlRepository with { ConnectionString = SqliteTestConstants.DEFAULT_CONNSTR };
    return (ITestingCtlRepository)await new TestingEfCtlRepository(() => 
        new SqliteCtlRepositoryDbContext(settings)).Initialise();
  }

}