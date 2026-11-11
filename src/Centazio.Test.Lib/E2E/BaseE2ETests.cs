using Centazio.Core.Runner;
using Centazio.Test.Lib.E2E.Sim;
using NUnit.Framework;

namespace Centazio.Test.Lib.E2E;

public abstract class BaseE2ETests {
  
  [Test] public async Task Run_e2e_simulation_and_tests_with_noop_change_notifier() {
    await new E2EEnvironment(new NoOpChangeNotifier(), new RandomSimulation(), await GetStorage()).RunSimulation();
  }
  
  [Test] public async Task Run_e2e_simulation_and_tests_with_instant_change_notifier() {
    await new E2EEnvironment(new InstantChangesNotifier(), new RandomSimulation(), await GetStorage()).RunSimulation();
  }

  [Test] public async Task Run_simple_single_step_scenario_with_instant_notifier() {
    await new E2EEnvironment(new InstantChangesNotifier(), new SimpleSingleStepSimulation(), await GetStorage()).RunSimulation();
  }

  [Test] public async Task Run_simple_single_step_scenario_with_inproc_notifier() {
    await new E2EEnvironment(new InProcessChangesNotifier(), new SimpleSingleStepSimulation(), await GetStorage()).RunSimulation();
  }
  
  [Test] public async Task Run_e2e_simulation_and_tests_with_inproc_change_notifier() {
    await new E2EEnvironment(new InProcessChangesNotifier(), new RandomSimulation(), await GetStorage()).RunSimulation();
  }
  
  [Test] public async Task Run_random_simulation_twice_with_same_seed_gives_identical_results() {
    var first = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new InProcessChangesNotifier());
    var second = await RunRandomSimulation(SimulationConstants.RANDOM_SEED, () => new InProcessChangesNotifier());
    AssertSameFingerprint(first, second);
  }

  protected abstract Task<ISimulationStorage> GetStorage();
  
  protected async Task<List<string>> RunRandomSimulation(int seed, Func<IChangesNotifier> notifier) {
    await using var env = new E2EEnvironment(notifier(), new RandomSimulation(), await GetStorage(), seed);
    return await env.RunSimulation();
  }
  
  protected static void AssertSameFingerprint(List<string> first, List<string> second) {
    var differs = Enumerable.Range(0, Math.Min(first.Count, second.Count)).Where(epoch => first[epoch] != second[epoch]).ToList();
    Assert.That(first, Is.Not.Empty);
    Assert.That(second, Has.Count.EqualTo(first.Count));
    Assert.That(differs, Is.Empty, $"replay diverged, first differing epoch [{differs.FirstOrDefault()}]");
  }
}