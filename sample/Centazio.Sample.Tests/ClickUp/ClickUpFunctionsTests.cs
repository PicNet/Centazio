using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpFunctionsTests {

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(), F.CtlRepo());
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(ClickUpConstants.ClickUpSystemName, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, ClickUpConstants.ClickUpTaskEntityName) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(), F.CtlRepo(), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    var (func, runner) = (new ClickUpPromoteFunction(stager, core, ctl), F.FuncRunner(ctl: ctl));
    var results = (await runner.RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)])).OpResults.Single();
    var ss = await ctl.GetSystemState(ClickUpConstants.ClickUpSystemName, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, CoreEntityTypes.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<ClickUpTask>().name).ToList();
    
    await using var db = core.Db();
    var coretasks = await core.Tasks(db).ToListAsync();
    
    Assert.That(results.Result.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  [Test] public async Task Test_Write() {
    var (core, ctl) = (await SampleTestHelpers.GetSampleCoreStorage(), F.CtlRepo());
    var func = new ClickUpWriteFunction(core, ctl, await GetApi());
    var results = await F.FuncRunner(ctl: ctl).RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)]);
    Assert.That(results, Is.Not.Null);
  }
  
  private async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new ClickUpReadFunction(stager, ctl, await GetApi());
    return (await F.FuncRunner(ctl: ctl).RunFunction(func, [new TimerChangeTrigger(func.Config.FunctionPollExpression ?? String.Empty)])).OpResults.Single().Result;
  }
  
  private async Task<ClickUpApi> GetApi() => new(F.Settings<Settings>(), await F.Secrets<Secrets>());

}