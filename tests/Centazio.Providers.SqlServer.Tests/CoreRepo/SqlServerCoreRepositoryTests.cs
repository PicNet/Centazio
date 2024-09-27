using Centazio.Core.Tests.CoreRepo;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageRepository> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

