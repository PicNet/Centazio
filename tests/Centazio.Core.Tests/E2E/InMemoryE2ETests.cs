using Centazio.Core.Runner;
using Centazio.Test.Lib.E2E;

namespace Centazio.Core.Tests.E2E;

[TestFixture] public class InMemoryE2ETests : BaseE2ETests {
  protected override Task<ISimulationStorage> GetStorage() => 
      Task.FromResult<ISimulationStorage>(new InMemorySimulationStorage());
  
  [Test] public async Task Run_random_simulation_twice_with_same_seed_gives_identical_results_with_instant_notifier() {
    var first = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new InstantChangesNotifier());
    var second = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new InstantChangesNotifier());
    AssertSameFingerprint(first, second);
  }
  
  [Test] public async Task Run_random_simulation_twice_with_same_seed_gives_identical_results_with_noop_notifier() {
    var first = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new NoOpChangeNotifier());
    var second = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new NoOpChangeNotifier());
    AssertSameFingerprint(first, second);
  }
  
  [Test] public async Task Run_random_simulation_with_a_different_seed_gives_different_results() {
    var first = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new InProcessChangesNotifier());
    var other = await RunRandomSimulation(SimulationConstants.RANDOM_SEED + 1, () => new InProcessChangesNotifier());
    Assert.That(other.Last(), Is.Not.EqualTo(first.Last()));
  }
}
