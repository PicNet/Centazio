using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryStateTests : BaseCtlRepositoryStateTests {
  protected override async Task<ICtlRepository> GetRepository() => 
      await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext(
          "Data Source=core_storage.db",
          nameof(Core.Ctl).ToLower(), 
          nameof(SystemState).ToLower(), 
          nameof(ObjectState).ToLower(), 
          nameof(Map.CoreToSysMap).ToLower()), new SqliteDbFieldsHelper()).Initialise();

}