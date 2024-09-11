namespace Centazio.Core.Tests.CoreRepo;

public class InMemoryCoreStorageRepositoryTests() : CoreStorageRepositoryDefaultTests(true) {

  protected override Task<ICoreStorageRepository> GetRepository() => Task.FromResult((ICoreStorageRepository) new TestingInMemoryCoreStorageRepository());

}