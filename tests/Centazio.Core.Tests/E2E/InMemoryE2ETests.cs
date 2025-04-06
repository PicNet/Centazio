using Centazio.Core.Runner;
using Centazio.Test.Lib;
using Centazio.Test.Lib.E2E;

namespace Centazio.Core.Tests.E2E;

public class InMemoryE2ETests {
  [Test] public async Task Run_e2e_simulation_and_tests() {
    // new InProcessChangesNotifier()
    await new E2EEnvironment(new InstantChangesNotifier(), new InMemorySimulationProvider(), F.Settings()).RunSimulation();
  }
} 