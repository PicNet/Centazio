using Centazio.Core.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Ctl;

public class InMemoryBaseCtlRepositoryTests : BaseCtlRepositoryStateTests {
  protected override Task<ICtlRepository> GetRepository() => Task.FromResult<ICtlRepository>(new InMemoryBaseCtlRepository());
}

public class InMemoryCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override Task<ITestingCtlRepository> GetRepository() => Task.FromResult<ITestingCtlRepository>(new TestingInMemoryBaseCtlRepository());
}