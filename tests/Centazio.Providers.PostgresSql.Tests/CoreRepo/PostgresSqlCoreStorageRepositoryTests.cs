using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Tests.CoreRepo;

public class PostgresSqlCoreStorageRepositoryTests : BaseCoreStorageRepositoryTests {
  
  protected override async Task<ITestingCoreStorage> GetRepository() {
    var connstr = await new PostgresSqlConnection().Init();
    return await new TestingEfCoreStorageRepository(() => new PostgresSqlCoreStorageRepositoryTestsDbContext(connstr)).Initalise();
  }

}

public class PostgresSqlCoreStorageRepositoryTestsDbContext(string connstr) : PostgresSqlDbContext(connstr) {
  protected override void CreateCentazioModel(ModelBuilder builder) {
    TestingEfCoreStorageRepository.CreateTestingCoreStorageEfModel(builder);
  }
}