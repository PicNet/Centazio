using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

