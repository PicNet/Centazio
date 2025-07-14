using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteCoreStorageRepositoryTests : BaseCoreStorageRepositoryTests {
  
  protected override async Task<ITestingCoreStorage> GetRepository() => 
      await new TestingEfCoreStorageRepository(
          () => new SqliteCoreStorageRepositoryTestsDbContext()).Initalise();

}

public class SqliteCoreStorageRepositoryTestsDbContext() : SqliteDbContext(SqliteTestConstants.DEFAULT_CONNSTR) {
  protected override void CreateCentazioModel(ModelBuilder builder) {
    TestingEfCoreStorageRepository.CreateTestingCoreStorageEfModel(builder);
  }
}