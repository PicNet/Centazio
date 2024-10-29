using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.CoreRepo;

public class InMemoryCoreStorageRepositoryTests() : BaseCoreStorageRepositoryTests(true) {

  protected override Task<ITestingCoreStorage> GetRepository() => Task.FromResult((ITestingCoreStorage) new TestingInMemoryCoreStorageRepository());

}