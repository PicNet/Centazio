using System.Text.Json;
using Centazio.Core.CoreRepo;
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
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(start, sys, obj, json1);
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    var exp = new PromoteOperationResult([], [], EOperationResult.Success, "", EResultType.List, 1) { ToPromote = result1.ToPromote };
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1)));
    Assert.That(func.CoreDb.Single(), Is.EqualTo(ToCore(json1)));
    
    // create two more entities and also include the previous one (without any changes
    var cust2 = new CrmCustomer(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new CrmCustomer(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    var staged23 = (await stager.Stage(TestingUtcDate.DoTick(), sys, obj, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2), SE(json3) }));
    Assert.That(result23.ToPromote, Is.EquivalentTo(new [] { (Staged: SE(json2), Core: ToCore(json2)), (Staged: SE(json3), Core: ToCore(json3)) }));
    var exp23 = new PromoteOperationResult([], [], EOperationResult.Success, "", EResultType.List, 2) { ToPromote = result23.ToPromote };
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 2)));
    Assert.That(func.CoreDb, Is.EquivalentTo(new [] { ToCore(json1), ToCore(json2), ToCore(json3) }));
  }
  
  [Test] public async Task Test_standalone_Promote_function_that_ignores_staged_entities() {
    // set up
    var (ctl, stager) = (TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), TestingFactories.PromoteRunner(stager));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new CrmCustomer(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(start, sys, obj, json1);
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    var exp = new PromoteOperationResult([], [], EOperationResult.Success, "", EResultType.List, 1) { ToPromote = result1.ToPromote };
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1)));
    Assert.That(func.CoreDb.Single(), Is.EqualTo(ToCore(json1)));
    
    // lets ignore all staged entities from now
    func.IgnoreNext = true;
    var cust2 = new CrmCustomer(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new CrmCustomer(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    var staged23 = (await stager.Stage(TestingUtcDate.DoTick(), sys, obj, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored (and not staged) as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2), SE(json3) }));
    Assert.That(result23.ToPromote.ToList(), Has.Count.EqualTo(0));
    Assert.That(result23.ToIgnore, Is.EquivalentTo(new [] { (SE(json2), (ValidString) "ignore"), (SE(json3), (ValidString) "ignore") }));
    var exp23 = new PromoteOperationResult([], [], EOperationResult.Success, "", EResultType.List, 2) { ToIgnore = result23.ToIgnore, ToPromote = result23.ToPromote };
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 2)));
    Assert.That(func.CoreDb.Single(), Is.EqualTo(ToCore(json1)));
  }
  
  private SystemState SS(DateTime start, DateTime updated) => new(sys, stg, true, start, ESystemStateStatus.Idle, updated, updated, updated);
  private ObjectState OS(DateTime start, DateTime updated, int len) => new(sys, stg, obj, true, start, EOperationResult.Success, EOperationAbortVote.Continue, 
      updated, updated, updated, updated, updated, "operation [CRM/Promote/CrmCustomer] completed [Success] message: ", len) { LastPayLoadType = len > 0 ? EResultType.List : EResultType.Empty };
  private StagedEntity SE(string json) => new(sys, obj, UtcDate.UtcNow, json, TestingFactories.TestingChecksum(json));
  private string Json(object o) => JsonSerializer.Serialize(o);
  private CoreCustomer ToCore(string json) => JsonSerializer.Deserialize<CoreCustomer>(json) ?? throw new Exception();
}

public class PromoteFunctionWithSinglePromoteCustomerOperation : AbstractPromoteFunction {

  public List<CoreCustomer> CoreDb { get; } = new(); 
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  public bool IgnoreNext { get; set; } 
  
  public PromoteFunctionWithSinglePromoteCustomerOperation() {
    Config = new(Constants.CrmSystemName, Constants.Promote, new ([
      new (Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), EvaluateCustomersToPromote, PromoteCustomers)
    ]));
  }

  public Task PromoteCustomers(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<ICoreEntity> entities) {
    CoreDb.AddRange(entities.Select(e => (CoreCustomer) e));
    return Task.CompletedTask;
  }

  private Task<PromoteOperationResult> EvaluateCustomersToPromote(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged) {
    var lst = staged.ToList();
    var cores = lst.Select(e => {
      ICoreEntity core = JsonSerializer.Deserialize<CoreCustomer>(e.Data) ?? throw new Exception();
      return (Staged: e, Core: core);
    });
    return Task.FromResult(new PromoteOperationResult(
        IgnoreNext ? [] : cores, 
        IgnoreNext ? lst.Select(e => (Entity: e, Reason: (ValidString) "ignore")) : [],
        EOperationResult.Success, 
        "",
        EResultType.List,
        lst.Count));
  }
}