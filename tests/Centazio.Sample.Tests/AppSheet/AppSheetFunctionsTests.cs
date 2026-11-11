using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Sample.AppSheet;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;
using Microsoft.Extensions.Time.Testing;

namespace Centazio.Sample.Tests.AppSheet;

public class AppSheetFunctionsTests {

  private FakeTimeProvider clock = null!;
  
  [SetUp] public void SetUp() => clock = F.Clock();

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(clock), F.CtlRepo(clock));
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(AppSheetConstants.AppSheetSystemName, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, AppSheetConstants.AppSheetTaskEntityName) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(clock.UtcNow()));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(clock), F.CtlRepo(clock), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    
    var func = new AppSheetPromoteFunction(stager, core, ctl, clock);
    var results = (await F.RunFunc(clock, func, ctl)).OpResults.Single().Result;
    var ss = await ctl.GetSystemState(AppSheetConstants.AppSheetSystemName, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, CoreEntityTypes.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<AppSheetTask>().Task).ToList();
    
    var coretasks = await core.Tasks();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(clock.UtcNow()));
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  private async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new AppSheetReadFunction(stager, ctl, clock, new AppSheetApi((await F.Settings<Settings>()).AppSheet, await F.Secrets<Secrets>()));
    var results = (await F.RunFunc(clock, func, ctl)).OpResults.Single().Result;
    return results;
  }

}