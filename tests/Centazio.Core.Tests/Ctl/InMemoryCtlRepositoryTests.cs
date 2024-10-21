using Centazio.Core.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Ctl;

public class InMemoryCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override Task<ICtlRepository> GetRepository() => Task.FromResult<ICtlRepository>(new InMemoryCtlRepository());
}

public class InMemoryCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override Task<ITestingCtlRepository> GetRepository() => Task.FromResult<ITestingCtlRepository>(new TestingInMemoryCtlRepository());
}