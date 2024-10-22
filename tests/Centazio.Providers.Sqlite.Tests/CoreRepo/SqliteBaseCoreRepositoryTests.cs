using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteBaseCoreRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqliteCoreStorageRepository().Initalise();
}

