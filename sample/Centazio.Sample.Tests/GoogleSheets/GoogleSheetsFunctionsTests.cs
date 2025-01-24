﻿using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Types;
using Centazio.Sample.GoogleSheets;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.Tests.GoogleSheets;

public class GoogleSheetsFunctionsTests {

  [Test] public async Task Test_Read() {
    var (stager, ctl) = (F.SeRepo(), F.CtlRepo());
    var results = await CreateAndRunReadFunction(stager, ctl);
    var ss = await ctl.GetSystemState(SC.Systems.GoogleSheets, LifecycleStage.Defaults.Read) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, SC.SystemEntities.GoogleSheets.TaskRow) ?? throw new Exception();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(stager.Contents, Is.Not.Empty);
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
  }

  [Test] public async Task Test_Promote() {
    var (stager, ctl, core) = (F.SeRepo(), F.CtlRepo(), await SampleTestHelpers.GetSampleCoreStorage());
    await CreateAndRunReadFunction(stager, ctl);
    
    var func = new GoogleSheetsPromoteFunction(stager, core, ctl);
    var results = (await func.RunFunction()).OpResults.Single();
    var ss = await ctl.GetSystemState(SC.Systems.GoogleSheets, LifecycleStage.Defaults.Promote) ?? throw new Exception();
    var os = await ctl.GetObjectState(ss, SC.CoreEntities.Task) ?? throw new Exception();
    var stagedtasks = stager.Contents.Select(se => se.Deserialise<GoogleSheetsTaskRow>().Value).ToList();
    await using var db = core.Db();
    var coretasks = await core.Tasks(db).ToListAsync();
    
    Assert.That(results.Result, Is.EqualTo(EOperationResult.Success));
    Assert.That(os.LastSuccessCompleted, Is.EqualTo(UtcDate.UtcNow));
    Assert.That(coretasks, Is.Not.Empty);
    Assert.That(coretasks.Select(t => t.Name).ToList(), Is.EquivalentTo(stagedtasks));
  }

  private static async Task<OperationResult> CreateAndRunReadFunction(TestingStagedEntityRepository stager, TestingInMemoryBaseCtlRepository ctl) {
    var func = new GoogleSheetsReadFunction(stager, ctl, new GoogleSheetsApi(F.Settings<SampleSettings>()));
    var results = (await func.RunFunction()).OpResults.Single();
    return results;
  }

}