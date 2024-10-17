using Centazio.Test.Lib.AbstractProviderTests;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageRepository> GetRepository() => await new TestingSqliteCoreStorageRepository().Initalise();
}

