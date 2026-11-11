using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.IntegrationTests.Write;

public class WriteFunctionTests {
  
  private FakeTimeProvider clock = null!;
  
  [SetUp] public void SetUp() => clock = F.Clock();
  
  [Test] public async Task Test_WriteFunction() {
    var (ctl, core) = (F.CtlRepo(clock), F.CoreRepo());
    var func = new TestingBatchWriteFunction(ctl, core, clock);
    
    var customer1 = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, C.Sys1Id1, new CoreEntity(C.CoreE1Id1, "1", "1", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum, clock.UtcNow());
    var customer2 = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, C.Sys1Id2, new CoreEntity(C.CoreE1Id2, "2", "2", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum, clock.UtcNow());
    var upsert1 = await core.Upsert(C.CoreEntityName, [customer1, customer2]);
    
    // update ids that were set by the write function
    // customer1 = customer1 with { Meta = customer1.Meta with { OriginalSystemId = upsert1[0].Meta.OriginalSystemId} };
    // customer2 = customer2 with { Meta = customer2.Meta with { OriginalSystemId = upsert1[0].Meta.OriginalSystemId} };
    
    var res1 = (WriteOperationResult) (await F.RunFunc(clock, func, ctl)).OpResults.Single().Result;
    var expresults1 = new [] { 
      Map.Create(C.System2Name, customer1.CoreEntity, clock.UtcNow()).SuccessCreate(func.Created[0].SystemId, func.Created[0].SystemEntityChecksum), 
      Map.Create(C.System2Name, customer2.CoreEntity, clock.UtcNow()).SuccessCreate(func.Created[1].SystemId, func.Created[1].SystemEntityChecksum) };
    var (created1, updated1) = (func.Created.ToList(), func.Updated.ToList());
    var stored2 = (await ctl.GetAllMaps()).Single(m => m.CoreId == customer2.CoreEntity.CoreId);
    func.Reset();
    
    clock.Tick();
    
    var customer22 = customer2.Update(C.System2Name, (CoreEntity) customer2.CoreEntity with { FirstName = "22" }, Helpers.TestingCoreEntityChecksum, clock.UtcNow());
    var upsert2 = await core.Upsert(C.CoreEntityName, [customer22]);
    var res2 = (WriteOperationResult) (await F.RunFunc(clock, func, ctl)).OpResults.Single().Result;
    var expresults2 = new [] { stored2.Update().SuccessUpdate(func.Updated.Single().SystemEntityChecksum) };
    var (created2, updated2) = (func.Created.ToList(), func.Updated.ToList());

    Assert.That(upsert1, Is.EquivalentTo([customer1, customer2]));
    Assert.That(res1.EntitiesUpdated, Is.Empty);

    Assert.That(res1.EntitiesCreated, Is.EquivalentTo(expresults1));
    Assert.That(created1, Is.EquivalentTo(expresults1));
    Assert.That(updated1, Is.Empty);
    
    Assert.That(upsert2, Is.EquivalentTo([customer22]));
    Assert.That(res2.EntitiesCreated, Is.Empty);
    Assert.That(res2.EntitiesUpdated, Is.EquivalentTo(expresults2));
    Assert.That(created2, Is.Empty);
    Assert.That(updated2, Is.EquivalentTo(expresults2));
  }
  
  [Test] public async Task Test_WriteFunction_writes_in_CoreId_order_whatever_order_core_storage_returns() {
    var (ctl, core) = (F.CtlRepo(clock), new ReversedCoreStorage(F.CoreRepo()));
    var func = new TestingBatchWriteFunction(ctl, core, clock);
    var ids = Enumerable.Range(0, 20).Select(idx => new CoreEntityId($"id{idx:D2}")).ToList();
    await core.Upsert(C.CoreEntityName, ids.Select(id => CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new(id.Value), new CoreEntity(id, id.Value, id.Value, new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum, clock.UtcNow())).ToList());
    
    clock.Tick();
    await F.RunFunc(clock, func, ctl);
    
    Assert.That(func.Created.Select(m => m.CoreId).ToList(), Is.EqualTo(ids));
  }
  
  [Test] public async Task Test_WriteFunction_error_handling() {
    var (ctl, core) = (F.CtlRepo(clock), F.CoreRepo());
    var func = new TestingBatchWriteFunction(ctl, core, clock);
    func.Throws = true;

    // add some data, as the write function will not be called if there is nothing to 'write'
    var ceam = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, C.Sys1Id1, new CoreEntity(C.CoreE1Id1, "1", "1", new DateOnly(2000, 1, 1)), Helpers.TestingCoreEntityChecksum, clock.UtcNow());
    await core.Upsert(C.CoreEntityName, [ceam]);
    
    var result = (ErrorOperationResult) (await F.RunFunc(clock, func, ctl)).OpResults.Single().Result;
    var sys = ctl.Systems.Single();
    var obj = ctl.Objects.Single();
    var allcusts = await core.GetAllCoreEntities();
    var maps = await ctl.GetAllMaps();

    Assert.That(result.Exception, Is.EqualTo(func.Thrown));
    Assert.That(result.AbortVote, Is.EqualTo(EOperationAbortVote.Abort));
    Assert.That(result.Result, Is.EqualTo(EOperationResult.Error));
    
    Assert.That(sys.Key, Is.EqualTo(new SystemStateKey(C.System2Name, LifecycleStage.Defaults.Write)));
    Assert.That(sys.Value, Is.EqualTo(SystemState.Create(C.System2Name, LifecycleStage.Defaults.Write, clock.UtcNow()).Completed(clock.UtcNow(), clock.UtcNow())));
    Assert.That(obj.Key, Is.EqualTo(new ObjectStateKey(C.System2Name, LifecycleStage.Defaults.Write, C.CoreEntityName)));
    Assert.That(obj.Value, Is.EqualTo(ObjectState.Create(C.System2Name, LifecycleStage.Defaults.Write, C.CoreEntityName, clock.UtcNow().AddMonths(-1), clock.UtcNow())
        .Error(clock.UtcNow(), EOperationAbortVote.Abort, obj.Value.LastRunMessage ?? String.Empty, func.Thrown?.ToString(), clock.UtcNow())));
    Assert.That(allcusts, Is.EquivalentTo([ceam.CoreEntity]));
    Assert.That(maps, Is.Empty);
  }
}

public class TestingBatchWriteFunction(ICtlRepository ctl, ICoreStorage core, TimeProvider clock) : WriteFunction(C.System2Name, core, ctl, clock) {

  public List<Map.Created> Created { get; } = [];
  public List<Map.Updated> Updated { get; } = [];
  public bool Throws { get; set; }
  public Exception? Thrown { get; private set; }
  
  protected override FunctionConfig GetFunctionConfiguration() => new([
    new WriteOperationConfig(System, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, CovertCoreEntitiesToSystemEntities, WriteEntitiesToTargetSystem)
  ]) { ThrowExceptions = false, ChecksumAlgorithm = Helpers.TestingChecksumAlgorithm };

  public void Reset() {
    Created.Clear();
    Updated.Clear();
  }
  
  private Task<ConvertCoresToSystemsResult> CovertCoreEntitiesToSystemEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var ccreate = tocreate.Select(e => new CoreSystemAndPendingCreateMap(e.CoreEntity, ToSysEnt(e.CoreEntity), e.Map, Helpers.TestingChecksumAlgorithm)).ToList();
    var cupdate = toupdate.Select(e => e.AddSystemEntity(ToSysEnt(e.CoreEntity, Guid.Parse(e.Map.SystemId.Value)), Helpers.TestingChecksumAlgorithm)).ToList();
    return Task.FromResult(new ConvertCoresToSystemsResult(ccreate, cupdate));
  }

  private Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (Throws) throw Thrown = new Exception("mock function error");
    return WriteOperationResult.Create<CoreEntity, System1Entity>(tocreate, toupdate, CreateEntities, UpdateEntities);
    
    Task<List<Map.Created>> CreateEntities(List<CoreSystemAndPendingCreateMap<CoreEntity, System1Entity>> _) => 
        Task.FromResult(Created.AddRangeAndReturn(tocreate.Select(m => m.SuccessCreate(m.SystemEntity.SystemId)).ToList()));

    Task<List<Map.Updated>> UpdateEntities(List<CoreSystemAndPendingUpdateMap<CoreEntity, System1Entity>> _) => 
        Task.FromResult(Updated.AddRangeAndReturn(toupdate.Select(m => m.SuccessUpdate()).ToList()));
  }
  
  private System1Entity ToSysEnt(ICoreEntity coreent, Guid? sysid = null) {
    var c = coreent.To<CoreEntity>();
    return new System1Entity(sysid ?? Guid.NewGuid(), c.FirstName, c.LastName, c.DateOfBirth, UtcNow);
  }
}

// returns the entities to write in descending `CoreId` order, as a database without an `ORDER BY` might
public class ReversedCoreStorage(ICoreStorage inner) : ICoreStorage {
  public Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) => inner.BeginTransaction(reuse);
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) => 
      (await inner.GetEntitiesToWrite(exclude, coretype, after)).OrderByDescending(e => e.CoreEntity.CoreId.Value, StringComparer.Ordinal).ToList();
  public Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => inner.GetExistingEntities(coretype, coreids);
  public Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => inner.GetChecksums(coretype, coreids);
  public Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) => inner.Upsert(coretype, entities);
  public ValueTask DisposeAsync() => inner.DisposeAsync();
}
