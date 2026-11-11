using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Ctl;

public class InMemoryBaseCtlRepositoryTests : BaseCtlRepositoryStateTests {
  protected override Task<ICtlRepository> GetRepository(TimeProvider clock) => Task.FromResult<ICtlRepository>(new InMemoryBaseCtlRepository(clock));
}

public class InMemoryCtlRepositoryMappingsTests : BaseCtlRepositoryMappingsTests {
  protected override Task<ITestingCtlRepository> GetRepository(TimeProvider clock) => Task.FromResult<ITestingCtlRepository>(new TestingInMemoryBaseCtlRepository(clock));
}