using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Types;
using Centazio.Sample.ClickUp;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpFunctionsTests {

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(), F.CtlRepo());
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(SC.Systems.ClickUp, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, SC.SystemEntities.ClickUp.Task) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(), F.CtlRepo(), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    var func = new ClickUpPromoteFunction(stager, core, ctl);
    var results = (await func.RunFunction()).OpResults.Single();
    var ss = await ctl.GetSystemState(SC.Systems.ClickUp, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, SC.CoreEntities.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<ClickUpTask>().name).ToList();
    
    await using var db = core.Db();
    var coretasks = await core.Tasks(db).ToListAsync();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  [Test] public async Task Test_Write() {
    var (core, ctl) = (await SampleTestHelpers.GetSampleCoreStorage(), F.CtlRepo());
    var func = new ClickUpWriteFunction(core, ctl, api);
    // todo: complete this test
    var results = await func.RunFunction();
    if (results is null) throw new Exception();
  }
  
  private async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new ClickUpReadFunction(stager, ctl, api);
    return (await func.RunFunction()).OpResults.Single();
  }
  
  private readonly ClickUpApi api = new(F.Settings<SampleSettings>().ClickUp, F.Secrets<SampleSecrets>());

}