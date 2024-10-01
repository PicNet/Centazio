using Centazio.Test.Lib.AbstractProviderTests;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageRepository> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

