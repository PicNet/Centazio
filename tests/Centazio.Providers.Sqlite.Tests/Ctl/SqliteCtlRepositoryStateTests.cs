using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() => 
      await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext("core_storage.db"), new SqliteDbFieldsHelper()).Initialise();

}