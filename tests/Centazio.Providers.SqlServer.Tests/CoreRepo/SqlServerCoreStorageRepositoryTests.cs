using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreStorageRepositoryTests : BaseCoreStorageRepositoryTests {
  
  protected override async Task<ITestingCoreStorage> GetRepository() {
    var connstr = (await SqlConn.GetInstance(false, await F.Secrets())).ConnStr;
    return await new TestingEfCoreStorageRepository(
        () => new SqlServerCoreStorageRepositoryTestsDbContect(connstr), 
        new SqlServerDbFieldsHelper()).Initalise();
  }

}

public class SqlServerCoreStorageRepositoryTestsDbContect(string connstr) : SqlServerDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) {
    TestingEfCoreStorageRepository.CreateTestingCoreStorageEfModel(builder);
  }

}