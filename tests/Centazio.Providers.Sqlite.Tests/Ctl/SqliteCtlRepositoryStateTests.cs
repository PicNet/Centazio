using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() {
    var settings = (await F.Settings()).CtlRepository with { ConnectionString = SqliteTestConstants.DEFAULT_CONNSTR };
    return await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext(settings), new SqliteDbFieldsHelper()).Initialise();
  }
}