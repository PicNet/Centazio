using centazio.core.Ctl;

namespace centazio.core.tests.Stage;

public class InMemoryCtlRepositoryTests : CtlRepositoryDefaultTests {

  protected override Task<ICtlRepository> GetRepository() => Task.FromResult<ICtlRepository>(new InMemoryCtlRepository());

}