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
    if (Env.IsGitHubActions()) return; // flaky on CI
    await new E2EEnvironment(new InProcessChangesNotifier(), new SimpleSingleStepSimulation(), await GetStorage()).RunSimulation();
  }
  
  [Test] public async Task Run_e2e_simulation_and_tests_with_inproc_change_notifier() {
    if (Env.IsGitHubActions()) return; // flaky on CI
    await new E2EEnvironment(new InProcessChangesNotifier(), new RandomSimulation(), await GetStorage()).RunSimulation();
  }

  protected abstract Task<ISimulationStorage> GetStorage();
}