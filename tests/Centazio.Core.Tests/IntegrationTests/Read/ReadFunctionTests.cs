using System.Collections;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Read;

public class ReadFunctionTests {
  
  private readonly SystemName sys = Constants.System1Name;
  private readonly LifecycleStage stg = LifecycleStage.Defaults.Read;
  private readonly SystemEntityTypeName sysent = Constants.SystemEntityName;
  
  [SetUp] public void SetUp() {
    UtcDate.Utc = new TestingUtcDate();
  }
  
  [Test] public async Task Test_standalone_read_function() {
    // set up
    var (start, ctl, stager) = (UtcDate.UtcNow, TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new ReadFunctionWithSingleReadCustomerOperation(), TestingFactories.ReadRunner(stager));
    var funcrunner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(func, oprunner, ctl);
    
    // run scenarios
    var (sys0, obj0) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged0 = (await stager.GetUnpromoted(sys, sysent, UtcDate.UtcNow.AddYears(-1))).ToList();
    
    // this run should be empty as no TestingUtcDate.DoTick
    var r1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged1 = (await stager.GetUnpromoted(sys, sysent, UtcDate.UtcNow.AddYears(-1))).ToList();
    
    // this should include the single customer added as a List result type
    var onetick = TestingUtcDate.DoTick();
    var r2 = (ListRecordsReadOperationResult) (await funcrunner.RunFunction()).OpResults.Single();
    var (sys2, obj2) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged2 = (await stager.GetUnpromoted(sys, sysent, UtcDate.UtcNow.AddYears(-1))).ToList();
    
    // should be empty as no time has passed and Cron expects max 1/sec
    var r3 = (await funcrunner.RunFunction()).OpResults; 
    var (sys3, obj3) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged3 = (await stager.GetUnpromoted(sys, sysent, UtcDate.UtcNow.AddYears(-1))).ToList();
    
    // validate results
    var expjson = Json.Serialize(DummyCrmApi.NewCust(0, onetick));
    Assert.That(r1, Is.EqualTo(new EmptyReadOperationResult()));
    Assert.That(r2.ToString(), Is.EqualTo(new ListRecordsReadOperationResult([expjson]).ToString()));
    Assert.That(r3, Is.Empty);
    
    // validate sys/obj states and staged entities
    Assert.That(new List<IEnumerable> { sys0, obj0, staged0 }, Is.All.Empty);
    
    Assert.That(sys1.Single(), Is.EqualTo(SS(start)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, 0)));
    Assert.That(staged1, Is.Empty);
    
    Assert.That(sys2.Single(), Is.EqualTo(SS(onetick)));
    Assert.That(obj2.Single(), Is.EqualTo(OS(onetick, 1)));
    Assert.That(staged2.Single(), Is.EqualTo(SE(staged2.Single().Id)));
    
    Assert.That(sys3.Single(), Is.EqualTo(SS(onetick)));
    Assert.That(obj3.Single(), Is.EqualTo(OS(onetick, 1)));
    Assert.That(staged3.Single(), Is.EqualTo(SE(staged3.Single().Id)));
    
    SystemState SS(DateTime updated) => new SystemState.Dto(sys, stg, true, start, ESystemStateStatus.Idle.ToString(), updated, updated, updated).ToBase();
    ObjectState OS(DateTime updated, int len) => new(sys, stg, sysent, true) {
      DateCreated = start,
      LastResult = EOperationResult.Success,
      LastAbortVote = EOperationAbortVote.Continue,
      DateUpdated = updated,
      LastStart = updated,
      LastSuccessStart = updated,
      LastSuccessCompleted = updated,
      LastCompleted = updated,
      LastRunMessage = $"operation [{sys}/{stg}/{sysent}] completed [Success] message: " + 
          (len == 0 ? "EmptyReadOperationResult" : $"ListRecordsReadOperationResult[{len}]")
    };
    StagedEntity SE(Guid? id = null) => new StagedEntity.Dto(id ?? Guid.CreateVersion7(), sys, sysent, onetick, expjson, Helpers.TestingStagedEntityChecksum(expjson)).ToBase();
  }
}

public class ReadFunctionWithSingleReadCustomerOperation : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  private readonly DummyCrmApi crmApi = new();
  
  public ReadFunctionWithSingleReadCustomerOperation() {
    Config = new(Constants.System1Name, LifecycleStage.Defaults.Read, [
      new(Constants.SystemEntityName, TestingDefaults.CRON_EVERY_SECOND, this)
    ]) { ChecksumAlgorithm = new Helpers.ChecksumAlgo() };
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var customers = await crmApi.GetCustomersUpdatedSince(config.Checkpoint);
    return customers.Any() ? 
        new ListRecordsReadOperationResult(customers)
        : new EmptyReadOperationResult();
  }

}