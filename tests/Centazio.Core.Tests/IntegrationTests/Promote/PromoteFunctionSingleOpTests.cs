using System.Text.Json;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Promote;

public class PromoteFunctionSingleOpTest {

  private readonly SystemName sys = Constants.CrmSystemName;
  private readonly LifecycleStage stg = Constants.Promote;
  private readonly ObjectName obj = Constants.CrmCustomer;
  
  [Test] public async Task Test_standalone_Promote_function() {
    // set up
    var (start, ctl, stager) = (UtcDate.UtcNow, TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), TestingFactories.PromoteRunner(stager));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // run scenarios
    var cust1 = new CrmCustomer(Guid.NewGuid(), "FN", "LN", new DateOnly(2000, 1, 2), UtcDate.UtcNow);
    await stager.Stage(TestingUtcDate.DoTick(), sys, obj, J(cust1));
    var result = (await funcrunner.RunFunction()).OpResults.Single();
    
    SystemState SS(DateTime updated) => new(sys, stg, true, start, ESystemStateStatus.Idle, updated, updated, updated);
    ObjectState OS(DateTime updated, int len) => new(sys, stg, obj, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
        updated, updated, updated, updated, updated, "operation [CRM/Read/CrmCustomer] completed [Success] message: " + (len == 0 ? "empty payload" : "list payload"), len) { LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty };
    // StagedEntity SE() => new(sys, obj, onetick, expjson, TestingFactories.TestingChecksum(expjson));
    string J(object o) => JsonSerializer.Serialize(o);
  }
}

public class PromoteFunctionWithSinglePromoteCustomerOperation : AbstractPromoteFunction {

  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  public bool IgnoreNext { get; set; } = false; 
  
  public PromoteFunctionWithSinglePromoteCustomerOperation() {
    Config = new(Constants.CrmSystemName, Constants.Promote, new ([
      new (Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), PromoteCustomers)
    ]));
  }
  
  private Task<PromoteOperationResult> PromoteCustomers(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged) {
    var lst = staged.ToList();
    return Task.FromResult(new PromoteOperationResult(
        IgnoreNext ? [] : lst, 
        IgnoreNext ? lst.Select(e => (Entity: e, Reason: (ValidString) "ignore")) : [],
        EOperationResult.Success, 
        "message",
        EResultType.List,
        lst.Count));
  }
}