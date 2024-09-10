using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public class InMemoryCoreStorageRepositoryTests() : CoreStorageRepositoryDefaultTests(true) {

  protected override Task<ICoreStorageRepository> GetRepository() => Task.FromResult(new InMemoryCoreStorageRepository() as ICoreStorageRepository);

}