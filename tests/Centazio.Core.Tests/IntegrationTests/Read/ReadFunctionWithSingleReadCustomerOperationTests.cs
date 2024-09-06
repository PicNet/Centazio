using System.Collections;
using System.Text.Json;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Read;

public class ReadFunctionWithSingleReadCustomerOperationTests {
  
  [Test] public async Task Test_standalone_read_function() {
    // set up
    var (start, ctl, stager) = (UtcDate.UtcNow, TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new ReadFunctionWithSingleReadCustomerOperation(), TestingFactories.ReadRunner(stager));
    var funcrunner = new FunctionRunner<ReadOperationConfig>(func, oprunner, ctl);

    async Task<IEnumerable<OperationResult>> RunFunc(bool isfirst = false) {
      var startstate = await ctl.GetSystemState(func.Config.System, func.Config.Stage);
      if (isfirst) Assert.That(startstate, Is.Null);
      else Assert.That(startstate!.Status, Is.EqualTo(ESystemStateStatus.Idle));
      var results = await funcrunner.RunFunction();
      Assert.That((await ctl.GetSystemState(func.Config.System, func.Config.Stage))!.Status, Is.EqualTo(ESystemStateStatus.Idle));
      return results.OpResults;
    }
    
    // run scenarios
    var (sys0, obj0) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged0 = await stager.Get(UtcDate.UtcNow.AddYears(-1), func.Config.System, Constants.CrmCustomer);
    
    // this run should be empty as no TestingUtcDate.DoTick
    var r1 = (await RunFunc(true)).Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged1 = await stager.Get(UtcDate.UtcNow.AddYears(-1), Constants.CrmSystemName, Constants.CrmCustomer);
    
    // this should include the single customer added as a List result type
    var onetick = TestingUtcDate.DoTick();
    var r2 = (await RunFunc()).Single();
    var (sys2, obj2) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged2 = await stager.Get(UtcDate.UtcNow.AddYears(-1), func.Config.System, Constants.CrmCustomer);
    
    var r3 = await RunFunc(); // should be empty as no time has passed and Cron expects max 1/sec
    var (sys3, obj3) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged3 = await stager.Get(UtcDate.UtcNow.AddYears(-1), func.Config.System, Constants.CrmCustomer);
    
    // validate results
    var expjson = JsonSerializer.Serialize(DummyCrmApi.NewCust(0, onetick));
    Assert.That(r1, Is.EqualTo(OperationResult.Success(String.Empty)));
    Assert.That(r2.ToString(), Is.EqualTo(OperationResult.Success(new [] { expjson }).ToString()));
    Assert.That(r3, Is.Empty);
    
    // validate sys/obj states and staged entities
    Assert.That(new IEnumerable[] { sys0, obj0, staged0 }, Is.All.Empty);
    
    Assert.That(sys1.Single(), Is.EqualTo(SS(start)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, 0)));
    Assert.That(staged1, Is.Empty);
    
    Assert.That(sys2.Single(), Is.EqualTo(SS(onetick)));
    Assert.That(obj2.Single(), Is.EqualTo(OS(onetick, 1)));
    Assert.That(staged2.Single(), Is.EqualTo(SE()));
    
    Assert.That(sys3.Single(), Is.EqualTo(SS(onetick)));
    Assert.That(obj3.Single(), Is.EqualTo(OS(onetick, 1)));
    Assert.That(staged3.Single(), Is.EqualTo(SE()));
    
    SystemState SS(DateTime updated) => new(func.Config.System, func.Config.Stage, true, start, ESystemStateStatus.Idle, updated, updated, updated);
    ObjectState OS(DateTime updated, int len) => new(func.Config.System, func.Config.Stage, Constants.CrmCustomer, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
        updated, updated, updated, updated, updated, "operation [CRM/Read/CrmCustomer] completed [Success] message: " + (len == 0 ? "empty payload" : "list payload"), len) { LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty };
    StagedEntity SE() => new(func.Config.System, Constants.CrmCustomer, onetick, expjson, TestingFactories.TestingChecksum(expjson));
  }
}

public class ReadFunctionWithSingleReadCustomerOperation : AbstractReadFunction {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  private readonly DummyCrmApi crmApi = new();
  
  public ReadFunctionWithSingleReadCustomerOperation() {
    Config = new(Constants.CrmSystemName, Constants.Read, new ([
      new (Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), ReadCustomers)
    ]));
  }
  
  private async Task<OperationResult> ReadCustomers(OperationStateAndConfig<ReadOperationConfig> config) {
    var customers = await crmApi.GetCustomersUpdatedSince(config.Checkpoint);
    return OperationResult.Success(customers);
  }
}