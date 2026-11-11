using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Test.Lib.InMemRepos;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Tests.Engine;

public class CentazioEngineTests {

  private readonly List<string> ENVIRONMENTS = [CentazioConstants.DEFAULT_ENVIRONMENT, "sim"];
  
  [Test] public async Task Test_in_memory_engine_resolves_in_memory_providers_from_settings() {
    var prov = await new InMemoryCentazioEngine(ENVIRONMENTS, F.Clock(), F.Ids()).Init(TestFunctions());
    
    Assert.That(prov.GetRequiredService<ICtlRepository>(), Is.TypeOf<InMemoryBaseCtlRepository>());
    Assert.That(prov.GetRequiredService<IStagedEntityRepository>(), Is.TypeOf<InMemoryStagedEntityRepository>());
  }
  
  [Test] public async Task Test_in_memory_engine_runs_a_function_with_its_own_clock() {
    var clock = F.Clock();
    var (engine, functype) = (new InMemoryCentazioEngine(ENVIRONMENTS, clock, F.Ids()), EmptyFunctionType());
    var prov = await engine.Init([functype]);
    
    var results = await engine.RunFunction(functype, []);
    var state = await GetSystemState(prov, functype);
    
    Assert.That(results.Message, Is.EqualTo("SuccessFunctionRunResults"));
    Assert.That(state.LastCompleted, Is.EqualTo(clock.UtcNow()));
  }
  
  [Test] public async Task Test_engines_in_one_process_each_use_their_own_clock() {
    var (clock1, clock2, functype) = (F.Clock(), F.Clock(), EmptyFunctionType());
    clock2.Tick(TimeSpan.FromDays(365));
    var (engine1, engine2) = (new InMemoryCentazioEngine(ENVIRONMENTS, clock1, F.Ids()), new InMemoryCentazioEngine(ENVIRONMENTS, clock2, F.Ids()));
    var (prov1, prov2) = (await engine1.Init([functype]), await engine2.Init([functype]));
    
    await engine1.RunFunction(functype, []);
    await engine2.RunFunction(functype, []);
    
    Assert.That((await GetSystemState(prov1, functype)).LastCompleted, Is.EqualTo(clock1.UtcNow()));
    Assert.That((await GetSystemState(prov2, functype)).LastCompleted, Is.EqualTo(clock2.UtcNow()));
  }
  
  private async Task<SystemState> GetSystemState(ServiceProvider prov, Type functype) {
    var func = (IRunnableFunction) prov.GetRequiredService(functype);
    return await prov.GetRequiredService<ICtlRepository>().GetSystemState(func.System, func.Stage) ?? throw new Exception();
  }
  
  // loaded by name, as `RunHostCommand` does, the test project does not reference the functions project
  private static List<Type> TestFunctions() => IntegrationsAssemblyInspector.GetRequiredCentazioFunctions(ReflectionUtils.LoadAssembly("Centazio.TestFunctions"), []);
  private static Type EmptyFunctionType() => TestFunctions().Single(t => t.Name == "EmptyFunction");
}
