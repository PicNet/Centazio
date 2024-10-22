using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerBaseCoreRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

