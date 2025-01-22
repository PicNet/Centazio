using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreStorageRepositoryTests : BaseCoreStorageRepositoryTests {
  
  protected override async Task<ITestingCoreStorage> GetRepository() {
    var connstr = (await SqlConn.GetInstance(false)).ConnStr;
    return await new TestingEfCoreStorageRepository(
        () => new SqlServerDbContext(connstr, TestingEfCoreStorageRepository.CreateTestingCoreStorageEfModel), 
        new SqlServerDbFieldsHelper()).Initalise();
  }

}