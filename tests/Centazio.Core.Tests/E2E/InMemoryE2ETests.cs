using Centazio.Test.Lib.E2E;

namespace Centazio.Core.Tests.E2E;

[TestFixture] public class InMemoryE2ETests : BaseE2ETests {
  protected override Task<ISimulationStorage> GetStorage() => 
      Task.FromResult<ISimulationStorage>(new InMemorySimulationStorage());
}
