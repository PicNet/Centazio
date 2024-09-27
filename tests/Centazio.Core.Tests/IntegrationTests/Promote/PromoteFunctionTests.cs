using System.Text.Json;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;
using F = Centazio.Core.Tests.TestingFactories;

namespace Centazio.Core.Tests.IntegrationTests.Promote;

public class PromoteFunctionTests {

  private readonly SystemName sys = Constants.System1Name;
  private readonly LifecycleStage stg = LifecycleStage.Defaults.Promote;
  private readonly ExternalEntityType externalname = Constants.ExternalEntityName;
  private readonly CoreEntityType corename = Constants.CoreEntityName;
  
  [Test] public async Task Test_standalone_Promote_function() {
    // set up
    var (ctl, stager, core, entitymap) = (F.CtlRepo(), F.SeStore(), F.CoreRepo(), F.EntitySysMap());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), F.PromoteRunner(stager, entitymap, core));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(sys, externalname, json1) ?? throw new Exception();
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.GetObjects<CoreEntityType>().Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.Query<CoreEntity>(corename, t => true)).Single(), Is.EqualTo(ToCore(json1)));
    
    // create two more entities and also include the previous one (without any changes
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(sys, externalname, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.GetObjects<CoreEntityType>().Values.ToList());

    // cust1 is ignored as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote, Is.EquivalentTo(new [] { new StagedAndCoreEntity(SE(json2, staged23[0].Id), ToCore(json2)), new StagedAndCoreEntity(SE(json3, staged23[1].Id), ToCore(json3)) }));
    Assert.That(result23.ToIgnore, Is.Empty); 
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 2, 0)));
    Assert.That(await core.Query<CoreEntity>(corename, t => true), Is.EquivalentTo(new [] { ToCore(json1), ToCore(json2), ToCore(json3) }));
  }
  
  [Test] public async Task Test_standalone_Promote_function_that_ignores_staged_entities() {
    // set up
    var (ctl, stager, core, entitymap) = (F.CtlRepo(), F.SeStore(), F.CoreRepo(), F.EntitySysMap());
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), F.PromoteRunner(stager, entitymap, core));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(sys, externalname, json1) ?? throw new Exception();
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.GetObjects<CoreEntityType>().Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(sys1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.Query<CoreEntity>(corename, t => true)).Single(), Is.EqualTo(ToCore(json1)));
    
    // lets ignore all staged entities from now
    func.IgnoreNext = true;
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(sys, externalname, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.GetObjects<CoreEntityType>().Values.ToList());

    // cust1 is ignored (and not staged) as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote.ToList(), Has.Count.EqualTo(0));
    Assert.That(result23.ToIgnore, Is.EquivalentTo(new [] { new StagedEntityAndIgnoreReason(SE(json2, staged23[0].Id), "ignore"), new StagedEntityAndIgnoreReason(SE(json3, staged23[1].Id), "ignore") }));
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 0, 2)));
    Assert.That((await core.Query<CoreEntity>(corename, t => true)).Single(), Is.EqualTo(ToCore(json1)));
  }
  
  private SystemState SS(DateTime start, DateTime updated) => (SystemState) new SystemState.Dto(sys, stg, true, start, ESystemStateStatus.Idle.ToString(), updated, updated, updated);
  private ObjectState<CoreEntityType> OS(DateTime start, DateTime updated, int promoted, int ignored) => new ObjectState<CoreEntityType>.Dto(sys, stg, corename, true) {
    DateCreated = start,
    LastResult = EOperationResult.Success.ToString(),
    LastAbortVote = EOperationAbortVote.Continue.ToString(),
    DateUpdated = updated,
    LastStart = updated,
    LastSuccessStart = updated,
    LastSuccessCompleted = updated,
    LastCompleted = updated,
    LastRunMessage = $"operation [{sys}/{stg}/{corename}] completed [Success] message: SuccessPromoteOperationResult Promote[{promoted}] Ignore[{ignored}]"
  }.ToObjectState<CoreEntityType>();
  private StagedEntity SE(string json, Guid? id = null) => (StagedEntity) new StagedEntity.Dto(id ?? Guid.NewGuid(), sys, corename, UtcDate.UtcNow, json, Helpers.TestingChecksum(json));
  private string Json(object o) => JsonSerializer.Serialize(o);
  private CoreEntity ToCore(string json) => JsonSerializer.Deserialize<CoreEntity>(json) ?? throw new Exception();
}

public class PromoteFunctionWithSinglePromoteCustomerOperation : AbstractFunction<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>, IEvaluateEntitiesToPromote {

  public override FunctionConfig<PromoteOperationConfig, CoreEntityType> Config { get; }
  public bool IgnoreNext { get; set; }
  
  public PromoteFunctionWithSinglePromoteCustomerOperation() {
    Config = new(Constants.System1Name, LifecycleStage.Defaults.Promote, new ([
      new (Constants.ExternalEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, IEnumerable<StagedEntity> staged) {
    var lst = staged.ToList();
    var cores = lst.Select(e => {
      var core = JsonSerializer.Deserialize<CoreEntity>(e.Data) ?? throw new Exception();
      return new StagedAndCoreEntity(e, core);
    }).ToList();
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(
        IgnoreNext ? [] : cores, 
        IgnoreNext ? lst.Select(e => new StagedEntityAndIgnoreReason(e, Reason: "ignore")).ToList() : []));
  }
}