using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.PostgresSql.Tests.Ctl;

public class PostgresSqlCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() {
    var connstr = await new PostgresSqlConnection().Init();
    return (ITestingCtlRepository) await new TestingEfCtlRepository(() => 
        new PostgresSqlCtlRepositoryDbContext(
            connstr,
            nameof(Core.Ctl).ToLower(), 
            nameof(SystemState).ToLower(), 
            nameof(ObjectState).ToLower(), 
            nameof(Map.CoreToSysMap).ToLower()), 
        new PostgresSqlDbFieldsHelper()).Initialise();
  }

}