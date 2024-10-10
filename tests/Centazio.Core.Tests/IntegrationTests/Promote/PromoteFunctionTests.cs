using System.Text.Json;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;
using Centazio.Test.Lib.CoreStorage;
using F = Centazio.Test.Lib.TestingFactories;

namespace Centazio.Core.Tests.IntegrationTests.Promote;

public class PromoteFunctionTests {

  private readonly SystemName system1 = Constants.System1Name;
  private readonly SystemName system2 = Constants.System2Name;
  private readonly LifecycleStage stage = LifecycleStage.Defaults.Promote;
  private readonly SystemEntityType system = Constants.SystemEntityName;
  private readonly CoreEntityType coretype = Constants.CoreEntityName;
  
  private TestingCtlRepository ctl;
  private TestingStagedEntityStore stager;
  private TestingInMemoryCoreStorageRepository core;
  private TestingInMemoryCoreToSystemMapStore entitymap;

  [SetUp] public void SetUp() {
    (ctl, stager, core, entitymap) = (F.CtlRepo(), F.SeStore(), F.CoreRepo(), F.CoreSysMap());
  }
      
  [Test] public async Task Test_standalone_Promote_function() {
    // set up
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), F.PromoteRunner(stager, entitymap, core));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(system1, system, json1) ?? throw new Exception();
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (s1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(s1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.Query<CoreEntity>(coretype, t => true)).Single(), Is.EqualTo(ToCore(json1)));
    
    // create two more entities and also include the previous one (without any changes
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(system1, system, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote.ToStagedCore(), Is.EquivalentTo(new [] { new Containers.StagedCore(SE(json2, staged23[0].Id), ToCore(json2)), new Containers.StagedCore(SE(json3, staged23[1].Id), ToCore(json3)) }));
    Assert.That(result23.ToIgnore, Is.Empty); 
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 2, 0)));
    Assert.That(await core.Query<CoreEntity>(coretype, t => true), Is.EquivalentTo(new [] { ToCore(json1), ToCore(json2), ToCore(json3) }));
  }
  
  [Test] public async Task Test_standalone_Promote_function_that_ignores_staged_entities() {
    // set up
    var (func, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(), F.PromoteRunner(stager, entitymap, core));
    var funcrunner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func, oprunner, ctl);
    
    // create single entity
    var start = TestingUtcDate.DoTick();
    var cust1 = new System1Entity(Guid.NewGuid(), "FN1", "LN1", new DateOnly(2000, 1, 2), start);
    var json1 = Json(cust1);
    var staged1 = await stager.Stage(system1, system, json1) ?? throw new Exception();
    var result1 = (await funcrunner.RunFunction()).OpResults.Single();
    var (s1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    
    var expse = SE(json1, staged1.Id);
    Assert.That(staged1, Is.EqualTo(expse));
    Assert.That(result1.ToPromote.Single().Staged, Is.EqualTo(staged1));
    Assert.That(result1.ToPromote.Single().Core, Is.EqualTo(ToCore(json1)));
    Assert.That(result1.ToIgnore, Is.Empty);
    var exp = new SuccessPromoteOperationResult(result1.ToPromote, result1.ToIgnore);
    Assert.That(result1, Is.EqualTo(exp));
    Assert.That(s1.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj1.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 1, 0)));
    Assert.That((await core.Query<CoreEntity>(coretype, t => true)).Single(), Is.EqualTo(ToCore(json1)));
    
    // lets ignore all staged entities from now
    func.IgnoreNext = true;
    var cust2 = new System1Entity(Guid.NewGuid(), "FN2", "LN2", new DateOnly(2000, 1, 2), start);
    var cust3 = new System1Entity(Guid.NewGuid(), "FN3", "LN3", new DateOnly(2000, 1, 2), start);
    var (json2, json3) = (Json(cust2), Json(cust3));
    TestingUtcDate.DoTick();
    var staged23 = (await stager.Stage(system1, system, [json1, json2, json3])).ToList();
    var result23 = (await funcrunner.RunFunction()).OpResults.Single();
    var (sys23, obj23) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());

    // cust1 is ignored (and not staged) as it has already been staged and checksum did not change
    Assert.That(staged23, Is.EquivalentTo(new [] { SE(json2, staged23[0].Id), SE(json3, staged23[1].Id) }));
    Assert.That(result23.ToPromote.ToList(), Has.Count.EqualTo(0));
    Assert.That(result23.ToIgnore, Is.EquivalentTo(new [] { new Containers.StagedIgnore(SE(json2, staged23[0].Id), "ignore"), new Containers.StagedIgnore(SE(json3, staged23[1].Id), "ignore") }));
    var exp23 = new SuccessPromoteOperationResult(result23.ToPromote, result23.ToIgnore);
    Assert.That(result23, Is.EqualTo(exp23));
    Assert.That(sys23.Single(), Is.EqualTo(SS(start, UtcDate.UtcNow)));
    Assert.That(obj23.Single(), Is.EqualTo(OS(start, UtcDate.UtcNow, 0, 2)));
    Assert.That((await core.Query<CoreEntity>(coretype, t => true)).Single(), Is.EqualTo(ToCore(json1)));
  }
  
  [Test] public async Task Test_that_bounce_backs_are_promoted_again_if_checksum_changes() {
    var (func1, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(system1, true), F.PromoteRunner(stager, entitymap, core));
    var runner1 = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func1, oprunner, ctl);
    
    var func2 = new PromoteFunctionWithSinglePromoteCustomerOperation(system2, true);
    var runner2 = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func2, oprunner, ctl);
    
    // For full scenario details see: AbstractCoreToSystemMapStoreTests#Reproduce_duplicate_mappings_found_in_simulation
    // Centazio creates map [System1:C1->E1]
    var se1 = await stager.Stage(system1, system, "1") ?? throw new Exception();
    var sysent1 = new System1Entity(Guid.NewGuid(), "First1", "Last1", DateOnly.MinValue, UtcDate.UtcNow);
    var c1 = sysent1.ToCoreEntity(Constants.CoreE1Id1, Constants.Sys1Id1);
    func1.NextResult = new SuccessPromoteOperationResult([new Containers.StagedSysCore(se1, sysent1, c1)], []);
    
    TestingUtcDate.DoTick();
    
    await runner1.RunFunction();
    var expkey1 = new Map.Key(coretype, Constants.CoreE1Id1, system1, Constants.Sys1Id1);
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1 }));
    Assert.That(CoresInDb, Is.EquivalentTo(new [] { c1 }));
    
    // Centazio writes C1 to System2 and creates map [System2:C1-E2]
    await entitymap.Create(Constants.System2Name, Constants.CoreEntityName, [ Map.Create(Constants.System2Name, c1).SuccessCreate(Constants.Sys1Id2, new(Guid.NewGuid().ToString()))]);
    var expkey2 = new Map.Key(coretype, Constants.CoreE1Id1, system2, Constants.Sys1Id2);
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1, expkey2 }));
    
    TestingUtcDate.DoTick();
    
    // System2 creates E2, Centazio reads/promotes E2/C2 and creates map [System2:C2-E2]
    var se2 = await stager.Stage(system2, system, "2") ?? throw new Exception();
    var sysent2 = new System1Entity(Guid.NewGuid(), "First2", "Last2", DateOnly.MinValue, UtcDate.UtcNow);
    var c2 = sysent2.ToCoreEntity(Constants.CoreE1Id2, Constants.Sys1Id2); 
    TestingUtcDate.DoTick();
    func2.NextResult = new SuccessPromoteOperationResult([new Containers.StagedSysCore(se2, sysent2, c2)], []);
    TestingUtcDate.DoTick();
    
    await runner2.RunFunction();
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1, expkey2 }));
    // Update is expected, this update should change the checksum (CHECKSUM2), the DateUpdated. Id and SourceId should remain the same 
    var expected = new CoreEntity(Constants.CoreE1Id1, "First2", "Last2", DateOnly.MinValue, c2.DateUpdated) { SourceId = Constants.Sys1Id1, LastUpdateSystem = Constants.System2Name };
    Assert.That(CoresInDb, Is.EquivalentTo(new [] { expected }));
  }
  
  [Test] public async Task Test_that_bounce_backs_are_ignored_if_checksum_is_the_same() {
    var (func1, oprunner) = (new PromoteFunctionWithSinglePromoteCustomerOperation(system1, true), F.PromoteRunner(stager, entitymap, core));
    var runner1 = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func1, oprunner, ctl);
    
    var func2 = new PromoteFunctionWithSinglePromoteCustomerOperation(system2, true);
    var runner2 = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(func2, oprunner, ctl);
    
    // For full scenario details see: AbstractCoreToSystemMapStoreTests#Reproduce_duplicate_mappings_found_in_simulation
    // Centazio creates map [System1:C1->E1]
    var se1 = await stager.Stage(system1, system, "1") ?? throw new Exception();
    var sysent1 = new System1Entity(Guid.NewGuid(), "First", "Last", DateOnly.MinValue, UtcDate.UtcNow);
    var c1 = sysent1.ToCoreEntity(Constants.CoreE1Id1, Constants.Sys1Id1);
    func1.NextResult = new SuccessPromoteOperationResult([new Containers.StagedSysCore(se1, sysent1, c1)], []);
    
    TestingUtcDate.DoTick();
    
    await runner1.RunFunction();
    var expkey1 = new Map.Key(coretype, Constants.CoreE1Id1, system1, Constants.Sys1Id1);
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1 }));
    Assert.That(CoresInDb, Is.EquivalentTo(new [] { c1 }));
    
    // Centazio writes C1 to System2 and creates map [System2:C1-E2]
    await entitymap.Create(Constants.System2Name, Constants.CoreEntityName, [ Map.Create(Constants.System2Name, c1).SuccessCreate(Constants.Sys1Id2, new(Guid.NewGuid().ToString()))]);
    var expkey2 = new Map.Key(coretype, Constants.CoreE1Id1, system2, Constants.Sys1Id2);
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1, expkey2 }));
    
    TestingUtcDate.DoTick();
    
    // System2 creates E2, Centazio reads/promotes E2/C2 and creates map [System2:C2-E2]
    var se2 = await stager.Stage(system2, system, "2") ?? throw new Exception();
    var sysent2 = new System1Entity(Guid.NewGuid(), "First", "Last", DateOnly.MinValue, UtcDate.UtcNow);
    var c2 = sysent2.ToCoreEntity(Constants.CoreE1Id2, Constants.Sys1Id2);
    TestingUtcDate.DoTick();
    func2.NextResult = new SuccessPromoteOperationResult([new Containers.StagedSysCore(se2, sysent2, c2)], []);
    TestingUtcDate.DoTick();
    
    await runner2.RunFunction();
    Assert.That((await entitymap.GetAll()).Select(m => m.Key).ToList(), Is.EquivalentTo(new [] { expkey1, expkey2 }));
    Assert.That(CoresInDb, Is.EquivalentTo(new [] { c1 })); // no changes to entity as checksum did not change
  }
  
  private List<ICoreEntity> CoresInDb => core.MemDb[coretype].Values.Select(e => e.Core).ToList();
  private SystemState SS(DateTime start, DateTime updated) => (SystemState) new SystemState.Dto(system1, stage, true, start, ESystemStateStatus.Idle.ToString(), updated, updated, updated);
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
  private StagedEntity SE(string json, Guid? id = null) => (StagedEntity) new StagedEntity.Dto(id ?? Guid.NewGuid(), system1, system, UtcDate.UtcNow, json, Helpers.TestingStagedEntityChecksum(json));
  private string Json(object o) => JsonSerializer.Serialize(o);
  private CoreEntity ToCore(string json) {
    var sysent = JsonSerializer.Deserialize<System1Entity>(json) ?? throw new Exception();
    return sysent.ToCoreEntity();
  }

}

public class PromoteFunctionWithSinglePromoteCustomerOperation : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {

  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  public bool IgnoreNext { get; set; }
  public PromoteOperationResult? NextResult { get; set; }
  
  public PromoteFunctionWithSinglePromoteCustomerOperation(SystemName? system=null, bool bidi=false) {
    Config = new(system ?? Constants.System1Name, LifecycleStage.Defaults.Promote, [
      new(Constants.SystemEntityName, Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = bidi }
    ]) { ChecksumAlgorithm = new Helpers.ChecksumAlgo() };
  }

  public List<Containers.StagedSys> DeserialiseStagedEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    if (NextResult is not null) return staged.Select(s => new Containers.StagedSys(s, new System1Entity(Guid.Empty, "N", "N", DateOnly.MinValue, DateTime.MinValue))).ToList();
    if (config.OpConfig.SystemEntityType.ToSystemEntityType != Constants.SystemEntityName) throw new NotSupportedException(config.OpConfig.Object.Value);
    return staged.ToStagedSys<System1Entity>();
  }

  public Task<PromoteOperationResult> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<Containers.StagedSysOptionalCore> staged) {
    if (NextResult is not null) return Task.FromResult(NextResult);
    
    var cores = staged.Select(e => e.SetCore(e.Sys.To<System1Entity>().ToCoreEntity())).ToList();
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(
        IgnoreNext ? [] : cores, 
        IgnoreNext ? staged.Select(e => new Containers.StagedIgnore(e.Staged, Ignore: "ignore")).ToList() : [])); 
  }
}