using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Sample.AppSheet;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Tests.AppSheet;

public class AppSheetFunctionsTests {

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(), F.CtlRepo());
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(AppSheetConstants.AppSheetSystemName, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, AppSheetConstants.AppSheetTaskEntityName) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(), F.CtlRepo(), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    
    var func = new AppSheetPromoteFunction(stager, core, ctl);
    var results = (await F.RunFunc(func, ctl: ctl)).OpResults.Single().Result;
    var ss = await ctl.GetSystemState(AppSheetConstants.AppSheetSystemName, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, CoreEntityTypes.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<AppSheetTask>().Task).ToList();
    
    await using var db = core.Db();
    var coretasks = await core.Tasks(db).ToListAsync();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  private static async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new AppSheetReadFunction(stager, ctl, new AppSheetApi(F.Settings<Settings>().AppSheet, F.Secrets<Secrets>()));
    var results = (await F.RunFunc(func, ctl: ctl)).OpResults.Single().Result;
    return results;
  }

}