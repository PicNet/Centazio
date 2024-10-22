using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.CoreRepo;

public class InMemoryBaseCoreStorageRepositoryTests() : BaseCoreStorageRepositoryTests(true) {

  protected override Task<ICoreStorageWithQuery> GetRepository() => Task.FromResult((ICoreStorageWithQuery) new TestingInMemoryCoreStorageRepository());

}