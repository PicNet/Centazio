using Centazio.Core.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Core.Tests.Ctl;

public class InMemoryCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override Task<ICtlRepository> GetRepository() => Task.FromResult<ICtlRepository>(new InMemoryCtlRepository());
}

public class InMemoryCtlMappingsTests : AbstractCtlMappingsTests {
  protected override Task<ITestingCtlRepository> GetStore() => Task.FromResult<ITestingCtlRepository>(new TestingInMemoryCtlRepository());
}