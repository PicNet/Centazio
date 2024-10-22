using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteBaseCoreRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqliteCoreStorageRepository().Initalise();
}

