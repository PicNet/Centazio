using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreStorageRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ITestingCoreStorage> GetRepository() {
    var connstr = await SqlConn.Instance.ConnStr();
    return await new TestingEfCoreStorageRepository(() => new SqlServerTestingCoreStorageDbContext(connstr), new SqlServerDbFieldsHelper()).Initalise();
  }

}

