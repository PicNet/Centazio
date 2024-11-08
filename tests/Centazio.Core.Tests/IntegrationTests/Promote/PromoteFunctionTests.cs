using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.IntegrationTests.Promote;

public class PromoteFunctionTests {

  private readonly SystemName system1 = C.System1Name;
  private readonly LifecycleStage stage = LifecycleStage.Defaults.Promote;
  private readonly SystemEntityTypeName system = C.SystemEntityName;
  private readonly CoreEntityTypeName coretype = C.CoreEntityName;
  
  private TestingInMemoryBaseCtlRepository ctl;
  private TestingStagedEntityRepository stager;
  private TestingInMemoryCoreStorageRepository core;

  [SetUp] public void SetUp() {
    (ctl, stager, core) = (F.CtlRepo(), F.SeRepo(), F.CoreRepo());
  }
  
  [Test] public async Task Test_generic_deserialisation() {
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), UtcDate.UtcNow);
    var json1 = Json.Serialize(cust1);
    var staged1 = await stager.Stage(system1, system, json1) ?? throw new Exception();
    
    var sysent = (ISystemEntity) Json.Deserialize(staged1.Data, typeof(System1Entity));
    Assert.That(sysent, Is.EqualTo(cust1));
  }
  
  [Test] public async Task Test_standalone_Promote_function() {
    // set up
    var func = new PromoteFunctionWithSinglePromoteCustomerOperation(stager, core, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json.Serialize(cust1);
    var staged1 = await stager.Stage(system1, system, json1) ?? throw new Exception();
    var result1 = (await func.RunFunction()).OpResults.Single();
    var (s1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().StagedEntity, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().CoreEntity, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(s1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.GetAllCoreEntities()).Single(), Is.EqualTo(ToCore(json1)));
    
    // create two more entities and also include the previous one (without any changes
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json.Serialize(cust2), Json.Serialize(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(system1, system, [json1, json2, json3])).ToList();
    var result23 = (await func.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote.Select(t => t.CoreEntity), Is.EquivalentTo(new [] { ToCore(json2), ToCore(json3) }));
    Assert.That(result23.ToPromote.Select(t => t.StagedEntity), Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToIgnore, Is.Empty); 
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 2, 0)));
    Assert.That(await core.GetAllCoreEntities(), Is.EquivalentTo(new [] { ToCore(json1), ToCore(json2), ToCore(json3) }));
  }
  
  [Test] public async Task Test_standalone_Promote_function_that_ignores_staged_entities() {
    // set up
    var func = new PromoteFunctionWithSinglePromoteCustomerOperation(stager, core, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json.Serialize(cust1);
    var staged1 = await stager.Stage(system1, system, json1) ?? throw new Exception();
    var result1 = (await func.RunFunction()).OpResults.Single();
    var (s1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().StagedEntity, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().CoreEntity, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(s1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.GetAllCoreEntities()).Single(), Is.EqualTo(ToCore(json1)));
    
    // lets ignore all staged entities from now
    func.IgnoreNext = true;
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json.Serialize(cust2), Json.Serialize(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(system1, system, [json1, json2, json3])).ToList();
    var result23 = (await func.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored (and not staged) as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote.ToList(), Has.Count.EqualTo(0));
    Assert.That(result23.ToIgnore, Is.EquivalentTo(new [] { (SE(json2, staged23[0].Id), new ValidString("ignore")), (SE(json3, staged23[1].Id), new ValidString("ignore")) }));
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 0, 2)));
    Assert.That((await core.GetAllCoreEntities()).Single(), Is.EqualTo(ToCore(json1)));
  }
  
  private SystemState SS(DateTime start, DateTime updated) => new SystemState.Dto(system1, stage, true, start, updated, ESystemStateStatus.Idle.ToString(), updated, updated).ToBase();
  private ObjectState OS(DateTime start, DateTime updated, int promoted, int ignored) => new(system1, stage, coretype, true) {
    DateCreated = start,
    LastResult = EOperationResult.Success,
    LastAbortVote = EOperationAbortVote.Continue,
    DateUpdated = updated,
    LastStart = updated,
    LastSuccessStart = updated,
    LastSuccessCompleted = updated,
    LastCompleted = updated,
    LastRunMessage = $"operation [{system1}/{stage}/{coretype}] completed [Success] message: SuccessPromoteOperationResult Promote[{promoted}] Ignore[{ignored}]"
  };
  private StagedEntity SE(string json, Guid? id = null) => new StagedEntity.Dto(id ?? Guid.NewGuid(), system1, system, UtcDate.UtcNow, json, Helpers.TestingStagedEntityChecksum(json)).ToBase();
  private CoreEntity ToCore(string json) {
    var sysent = Json.Deserialize<System1Entity>(json).ToCoreEntity();
    return sysent;
  }

}

public class PromoteFunctionWithSinglePromoteCustomerOperation(IStagedEntityRepository stager, ICoreStorage core, ICtlRepository ctl, SystemName? system=null, bool bidi=false) : PromoteFunction(system ?? C.System1Name, stager, core, ctl) {
  
  public bool IgnoreNext { get; set; }
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new(typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = bidi }
  ]) { ChecksumAlgorithm = new Helpers.ChecksumAlgo() };

  public override Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results = toeval.Select(eval => {
      if (IgnoreNext) return eval.MarkForIgnore(new("ignore"));
      var core = eval.SystemEntity.To<System1Entity>().ToCoreEntity();
      return eval.MarkForPromotion(eval, config.State.System, core, Config.ChecksumAlgorithm.Checksum);
    }).ToList();
    return Task.FromResult(results); 
  }
  

}