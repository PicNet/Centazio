using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    return (ITestingCtlRepository)await new TestingEfCtlRepository(() => 
        new SqliteCtlRepositoryDbContext(
            "Data Source=centazio_ctl.db",
            nameof(Core.Ctl).ToLower(), 
            nameof(SystemState).ToLower(), 
            nameof(ObjectState).ToLower(), 
            nameof(Map.CoreToSysMap).ToLower()), 
        new SqliteDbFieldsHelper()).Initialise();
  }

}