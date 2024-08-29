using Centazio.Core.Ctl;

namespace Centazio.Core.Tests.Ctl;

public class InMemoryCtlRepositoryTests : CtlRepositoryDefaultTests {

  protected override Task<ICtlRepository> GetRepository() => Task.FromResult<ICtlRepository>(new InMemoryCtlRepository());

}