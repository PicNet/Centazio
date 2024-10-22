using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerBaseCoreRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqlServerCoreStorageRepository().Initalise();
}

