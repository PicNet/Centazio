using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteCoreStorageRepositoryTests : BaseCoreStorageRepositoryTests {
  
  protected override async Task<ITestingCoreStorage> GetRepository() => await new TestingEfCoreStorageRepository(() => new SqliteTestingCoreStorageDbContext(), new SqliteDbFieldsHelper()).Initalise();
}

