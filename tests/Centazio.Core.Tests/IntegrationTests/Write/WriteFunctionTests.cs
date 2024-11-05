using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Write;

public class WriteFunctionTests {
  [Test] public async Task Test_WriteFunction() {
    var (ctl, core) = (F.CtlRepo(), F.CoreRepo());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<WriteOperationConfig>(ctl, core));
    var funcrunner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(oprunner, ctl);
    
    var customer1 = CoreEntityAndMeta.Create(C.System1Name, C.Sys1Id1, new CoreEntity(C.CoreE1Id1, "1", "1", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum);
    var customer2 = CoreEntityAndMeta.Create(C.System1Name, C.Sys1Id2, new CoreEntity(C.CoreE1Id2, "2", "2", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum);
    var upsert1 = await core.Upsert(C.CoreEntityName, [customer1, customer2]);
    
    // update ids that were set by the write function
    // customer1 = customer1 with { Meta = customer1.Meta with { OriginalSystemId = upsert1[0].Meta.OriginalSystemId} };
    // customer2 = customer2 with { Meta = customer2.Meta with { OriginalSystemId = upsert1[0].Meta.OriginalSystemId} };
    
    var res1 = (await funcrunner.RunFunction(func)).OpResults.Single();
    var expresults1 = new [] { 
      Map.Create(C.System2Name, customer1.CoreEntity).SuccessCreate(func.Created[0].SystemId, WftHelpers.ToSeCs(customer1.CoreEntity)), 
      Map.Create(C.System2Name, customer2.CoreEntity).SuccessCreate(func.Created[1].SystemId, WftHelpers.ToSeCs(customer2.CoreEntity)) };
    var (created1, updated1) = (func.Created.ToList(), func.Updated.ToList());
    func.Reset();
    
    TestingUtcDate.DoTick();
    
    var customer22 = customer2.Update(C.System2Name, (CoreEntity) customer2.CoreEntity with { FirstName = "22" }, Helpers.TestingCoreEntityChecksum);
    var upsert2 = await core.Upsert(C.CoreEntityName, [customer22]);
    var res2 = (await funcrunner.RunFunction(func)).OpResults.Single();
    var expresults2 = new [] { expresults1[1].Update().SuccessUpdate(WftHelpers.ToSeCs(customer22.CoreEntity)) };
    var (created2, updated2) = (func.Created.ToList(), func.Updated.ToList());

    Assert.That(upsert1, Is.EquivalentTo(new [] { customer1, customer2 }));
    Assert.That(res1.EntitiesUpdated, Is.Empty);

    Assert.That(res1.EntitiesCreated, Is.EquivalentTo(expresults1));
    Assert.That(created1, Is.EquivalentTo(expresults1));
    Assert.That(updated1, Is.Empty);
    
    Assert.That(upsert2, Is.EquivalentTo(new [] { customer22 }));
    Assert.That(res2.EntitiesCreated, Is.Empty);
    Assert.That(res2.EntitiesUpdated, Is.EquivalentTo(expresults2));
    Assert.That(created2, Is.Empty);
    Assert.That(updated2, Is.EquivalentTo(expresults2));
  }
  
  [Test] public async Task Test_WriteFunction_error_handling() {
    var (ctl, core) = (F.CtlRepo(), F.CoreRepo());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<WriteOperationConfig>(ctl, core));
    func.Throws = true;
    var funcrunner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(oprunner, ctl);

    // add some data, as the write function will not be called if there is nothing to 'write'
    var ceam = CoreEntityAndMeta.Create(C.System1Name, C.Sys1Id1, new CoreEntity(C.CoreE1Id1, "1", "1", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum);
    await core.Upsert(C.CoreEntityName, [ceam]);
    
    var result = (ErrorWriteOperationResult) (await funcrunner.RunFunction(func)).OpResults.Single();
    var sys = ctl.Systems.Single();
    var obj = ctl.Objects.Single();
    var allcusts = await core.GetAllCoreEntities();
    var maps = await ctl.GetAllMaps();

    Assert.That(result.EntitiesUpdated, Is.Empty);
    Assert.That(result.EntitiesCreated, Is.Empty);
    Assert.That(result.Exception, Is.EqualTo(func.Thrown));
    Assert.That(result.TotalChanges, Is.EqualTo(0));
    Assert.That(result.AbortVote, Is.EqualTo(EOperationAbortVote.Abort));
    Assert.That(result.Result, Is.EqualTo(EOperationResult.Error));
    
    Assert.That(sys.Key, Is.EqualTo((C.System2Name, LifecycleStage.Defaults.Write)));
    Assert.That(sys.Value, Is.EqualTo(SystemState.Create(C.System2Name, LifecycleStage.Defaults.Write).Completed(UtcDate.UtcNow)));
    Assert.That(obj.Key, Is.EqualTo((C.System2Name, LifecycleStage.Defaults.Write, C.CoreEntityName)));
    Assert.That(obj.Value, Is.EqualTo(ObjectState.Create(C.System2Name, LifecycleStage.Defaults.Write, C.CoreEntityName).Error(UtcDate.UtcNow, EOperationAbortVote.Abort, obj.Value.LastRunMessage ?? String.Empty, func.Thrown?.ToString())));
    Assert.That(allcusts, Is.EquivalentTo(new [] { ceam.CoreEntity }));
    Assert.That(maps, Is.Empty);
  }
}

public class TestingBatchWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {

  public List<Map.Created> Created { get; } = [];
  public List<Map.Updated> Updated { get; } = [];
  public bool Throws { get; set; }
  public Exception? Thrown { get; private set; }
  public override FunctionConfig<WriteOperationConfig> Config { get; }

  public TestingBatchWriteFunction() {
    Config = new FunctionConfig<WriteOperationConfig>(C.System2Name, LifecycleStage.Defaults.Write, [
      new(C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, this)
    ]) { ThrowExceptions = false, ChecksumAlgorithm = new Helpers.ChecksumAlgo() };
  }

  public void Reset() {
    Created.Clear();
    Updated.Clear();
  }
  
  public Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreEntitiesToSystemEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var ccreate = tocreate.Select(e => new CoreSystemAndPendingCreateMap(e.CoreEntity, WftHelpers.ToSe(e.CoreEntity), e.Map)).ToList();
    var cupdate = toupdate.Select(e => e.AddSystemEntity(WftHelpers.ToSe(e.CoreEntity, Guid.Parse(e.Map.SystemId.Value)))).ToList();
    return Task.FromResult(new CovertCoreEntitiesToSystemEntitiesResult(ccreate, cupdate));
  }

  public Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (Throws) throw Thrown = new Exception("mock function error");
    var news = tocreate.Select(m => m.Map.SuccessCreate(m.SystemEntity.SystemId, Helpers.TestingSystemEntityChecksum(m.SystemEntity))).ToList();
    var updates = toupdate.Select(m => m.Map.SuccessUpdate(Helpers.TestingSystemEntityChecksum(m.SystemEntity))).ToList();
    Created.AddRange(news);
    Updated.AddRange(updates);
    return Task.FromResult<WriteOperationResult>(new SuccessWriteOperationResult(news, updates));
  }
 
}

internal static class WftHelpers {
  public static SystemEntityChecksum ToSeCs(ICoreEntity coreent) => Helpers.TestingSystemEntityChecksum(ToSe(coreent));
  public static System1Entity ToSe(ICoreEntity coreent, Guid? sysid = null) {
    var c = coreent.To<CoreEntity>();
    return new System1Entity(sysid ?? Guid.NewGuid(), c.FirstName, c.LastName, c.DateOfBirth, UtcDate.UtcNow);
  }

}