using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqliteCoreStorageRepository().Initalise();
}

