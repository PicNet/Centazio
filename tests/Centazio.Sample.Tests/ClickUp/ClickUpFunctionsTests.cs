using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;
using Microsoft.Extensions.Time.Testing;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpFunctionsTests {

  private FakeTimeProvider clock = null!;
  
  [SetUp] public void SetUp() => clock = F.Clock();

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(clock), F.CtlRepo(clock));
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(ClickUpConstants.ClickUpSystemName, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, ClickUpConstants.ClickUpTaskEntityName) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(clock.UtcNow()));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(clock), F.CtlRepo(clock), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    var (func, runner) = (new ClickUpPromoteFunction(stager, core, ctl, clock), await F.FuncRunner(clock, ctl));
    var results = (await runner.RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)])).OpResults.Single();
    var ss = await ctl.GetSystemState(ClickUpConstants.ClickUpSystemName, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, CoreEntityTypes.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<ClickUpTask>().name).ToList();
    
    var coretasks = await core.Tasks();
    
    Assert.That(results.Result.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(clock.UtcNow()));
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  [Test] public async Task Test_Write() {
    var (core, ctl) = (await SampleTestHelpers.GetSampleCoreStorage(), F.CtlRepo(clock));
    var func = new ClickUpWriteFunction(core, ctl, clock, await GetApi());
    var results = await (await F.FuncRunner(clock, ctl)).RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)]);
    Assert.That(results, Is.Not.Null);
  }
  
  private async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new ClickUpReadFunction(stager, ctl, clock, await GetApi());
    return (await (await F.FuncRunner(clock, ctl)).RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)])).OpResults.Single().Result;
  }
  
  private async Task<ClickUpApi> GetApi() => new(await F.Settings<Settings>(), await F.Secrets<Secrets>());

}