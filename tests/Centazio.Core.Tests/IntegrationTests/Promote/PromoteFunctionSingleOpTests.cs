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
    var (ctl, stager) = (TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), TestingFactories.PromoteRunner(stager));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new CrmCustomer(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var staged1 = await stager.Stage(start, sys, obj, Json(cust1));
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(Json(cust1));
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.Promoted.Single(), Is.EqualTo(staged1));
    var exp = new PromoteOperationResult(new List<StagedEntity> {expse}, [], EOperationResult.Success, "", EResultType.List, 1) { Promoted = result1.Promoted };
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(UtcDate.UtcNow, 1)));
    
    // create two more entities and also include the previous one (without any changes
    var cust2 = new CrmCustomer(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new CrmCustomer(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var staged23 = (await stager.Stage(TestingUtcDate.DoTick(), sys, obj, [Json(cust1), Json(cust2), Json(cust3)])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(Json(cust2)), SE(Json(cust3)) }));
    Assert.That(result23.Promoted, Is.EquivalentTo(new [] { SE(Json(cust2)), SE(Json(cust3)) }));
    var exp23 = new PromoteOperationResult(new List<StagedEntity> {expse}, [], EOperationResult.Success, "", EResultType.List, 2) { Promoted = result23.Promoted };
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(UtcDate.UtcNow, 2)));
    
    SystemState SS(DateTime updated) => new(sys, stg, true, start, ESystemStateStatus.Idle, updated, updated, updated);
    ObjectState OS(DateTime updated, int len) => new(sys, stg, obj, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
        updated, updated, updated, updated, updated, "operation [CRM/Promote/CrmCustomer] completed [Success] message: ", len) { LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty };
    StagedEntity SE(string json) => new(sys, obj, UtcDate.UtcNow, json, TestingFactories.TestingChecksum(json));
    string Json(object o) => JsonSerializer.Serialize(o);
  }
  
  [Test] public async Task Test_standalone_Promote_function_that_ignores_staged_entities() {
    // set up
    var (ctl, stager) = (TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), TestingFactories.PromoteRunner(stager));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new CrmCustomer(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var staged1 = await stager.Stage(start, sys, obj, Json(cust1));
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(Json(cust1));
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.Promoted.Single(), Is.EqualTo(staged1));
    var exp = new PromoteOperationResult(new List<StagedEntity> {expse}, [], EOperationResult.Success, "", EResultType.List, 1) { Promoted = result1.Promoted };
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(UtcDate.UtcNow, 1)));
    
    // lets ignore all staged entities from now
    func.IgnoreNext = true;
    var cust2 = new CrmCustomer(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new CrmCustomer(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var staged23 = (await stager.Stage(TestingUtcDate.DoTick(), sys, obj, [Json(cust1), Json(cust2), Json(cust3)])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored (and not staged) as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(Json(cust2)), SE(Json(cust3)) }));
    Assert.That(result23.Promoted.ToList(), Has.Count.EqualTo(0));
    Assert.That(result23.Ignored, Is.EquivalentTo(new [] { (SE(Json(cust2)), (ValidString) "ignore"), (SE(Json(cust3)), (ValidString) "ignore") }));
    var exp23 = new PromoteOperationResult([], [], EOperationResult.Success, "", EResultType.List, 2) { Ignored = result23.Ignored, Promoted = result23.Promoted };
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(UtcDate.UtcNow, 2)));
    
    SystemState SS(DateTime updated) => new(sys, stg, true, start, ESystemStateStatus.Idle, updated, updated, updated);
    ObjectState OS(DateTime updated, int len) => new(sys, stg, obj, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
        updated, updated, updated, updated, updated, "operation [CRM/Promote/CrmCustomer] completed [Success] message: ", len) { LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty };
    StagedEntity SE(string json) => new(sys, obj, UtcDate.UtcNow, json, TestingFactories.TestingChecksum(json));
    string Json(object o) => JsonSerializer.Serialize(o);
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
        "",
        EResultType.List,
        lst.Count));
  }
}