using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.AbstractProviderTests;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Core.Tests.CoreRepo;

public class InMemoryCoreStorageRepositoryTests() : CoreStorageRepositoryDefaultTests(true) {

  protected override Task<ICoreStorageWithQuery> GetRepository() => Task.FromResult((ICoreStorageWithQuery) new TestingInMemoryCoreStorageRepository());

}