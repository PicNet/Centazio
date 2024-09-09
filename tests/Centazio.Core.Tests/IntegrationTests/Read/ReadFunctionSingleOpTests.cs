using System.Collections;
using System.Text.Json;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Read;

public class ReadFunctionSingleOpTests {
  
  private readonly SystemName sys = Constants.CrmSystemName;
  private readonly LifecycleStage stg = Constants.Read;
  private readonly ObjectName obj = Constants.CrmCustomer;
  
  [Test] public async Task Test_standalone_read_function() {
    // set up
    var (start, ctl, stager) = (UtcDate.UtcNow, TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new ReadFunctionWithSingleReadCustomerOperation(), TestingFactories.ReadRunner(stager));
    var funcrunner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(func, oprunner, ctl);
    
    // run scenarios
    var (sys0, obj0) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged0 = await stager.Get(UtcDate.UtcNow.AddYears(-1), sys, obj);
    
    // this run should be empty as no TestingUtcDate.DoTick
    var r1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged1 = await stager.Get(UtcDate.UtcNow.AddYears(-1), sys, obj);
    
    // this should include the single customer added as a List result type
    var onetick = TestingUtcDate.DoTick();
    var r2 = (ListRecordsReadOperationResult) (await funcrunner.RunFunction()).OpResults.Single();
    var (sys2, obj2) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged2 = await stager.Get(UtcDate.UtcNow.AddYears(-1), sys, obj);
    
    // should be empty as no time has passed and Cron expects max 1/sec
    var r3 = (await funcrunner.RunFunction()).OpResults; 
    var (sys3, obj3) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged3 = await stager.Get(UtcDate.UtcNow.AddYears(-1), sys, obj);
    
    // validate results
    var expjson = JsonSerializer.Serialize(DummyCrmApi.NewCust(0, onetick));
    Assert.That(r1, Is.EqualTo(new EmptyReadOperationResult("")));
    Assert.That(r2.ToString(), Is.EqualTo(new ListRecordsReadOperationResult(new List<string> { expjson }, "").ToString()));
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
    
    SystemState SS(DateTime updated) => new(sys, stg, true, start, ESystemStateStatus.Idle, updated, updated, updated);
    ObjectState OS(DateTime updated, int len) => new(sys, stg, obj, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
        updated, updated, updated, updated, updated, "operation [CRM/Read/CrmCustomer] completed [Success] message: ", len) { 
      LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty 
    };
    StagedEntity SE() => new(sys, obj, onetick, expjson, TestingFactories.TestingChecksum(expjson));
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
  
  private async Task<ReadOperationResult> ReadCustomers(OperationStateAndConfig<ReadOperationConfig> config) {
    var customers = await crmApi.GetCustomersUpdatedSince(config.Checkpoint);
    return customers.Any() ? 
        new ListRecordsReadOperationResult(customers, "")
        : new EmptyReadOperationResult("");
  }
}