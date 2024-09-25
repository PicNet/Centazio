using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;
using F = Centazio.Core.Tests.TestingFactories;

namespace Centazio.Core.Tests.IntegrationTests.Write;

public class WriteFunctionTests {
  [Test] public async Task Test_WriteFunction() {
    var (ctl, core, entitymap) = (F.CtlRepo(), F.CoreRepo(), F.EntitySysMap());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<BatchWriteOperationConfig<CoreCustomer>>(entitymap, core));
    var funcrunner = new FunctionRunner<BatchWriteOperationConfig<CoreCustomer>, WriteOperationResult<CoreCustomer>>(func, oprunner, ctl);
    
    var customer1 = new CoreCustomer(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "1", "1", new DateOnly(2000, 1, 1), UtcDate.UtcNow);
    var customer2 = new CoreCustomer(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "2", "2", new DateOnly(2000, 1, 1), UtcDate.UtcNow);
    var upsert1 = await core.Upsert(new [] { customer1, customer2 });
    var res1 = (await funcrunner.RunFunction()).OpResults.Single();
    var expresults1 = new [] { 
      (Core: customer1, Map: EntityIntraSysMap.Create(customer1, Constants.FinSystemName).SuccessCreate(customer1.SourceId) ), 
      (Core: customer2, Map: EntityIntraSysMap.Create(customer2, Constants.FinSystemName).SuccessCreate(customer2.SourceId) ) };
    var (created1, updated1) = (func.Created.ToList(), func.Updated.ToList());
    func.Reset();
    
    TestingUtcDate.DoTick();
    
    var customer22 = customer2 with { Checksum = Guid.NewGuid().ToString(), FirstName = "22", DateUpdated = UtcDate.UtcNow };
    var upsert2 = await core.Upsert(new [] { customer22 });
    var res2 = (await funcrunner.RunFunction()).OpResults.Single();
    var expresults2 = new [] { (Core: customer22, Map: expresults1[1].Map.Update().SuccessUpdate() ) };
    var (created2, updated2) = (func.Created.ToList(), func.Updated.ToList());
    func.Reset();

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
    var (ctl, core, entitymap) = (F.CtlRepo(), F.CoreRepo(), F.EntitySysMap());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<BatchWriteOperationConfig<CoreCustomer>>(entitymap, core));
    func.Throws = true;
    var funcrunner = new FunctionRunner<BatchWriteOperationConfig<CoreCustomer>, WriteOperationResult<CoreCustomer>>(func, oprunner, ctl);
    
    var result = (ErrorWriteOperationResult<CoreCustomer>) (await funcrunner.RunFunction()).OpResults.Single();
    var sys = ctl.Systems.Single();
    var obj = ctl.Objects.Single();
    var allcusts = await core.Query<CoreCustomer>(c => true);
    var maps = await entitymap.GetAll();

    Assert.That(result.EntitiesUpdated, Is.Empty);
    Assert.That(result.EntitiesCreated, Is.Empty);
    Assert.That(result.Exception, Is.EqualTo(func.Thrown));
    Assert.That(result.TotalChanges, Is.EqualTo(0));
    Assert.That(result.AbortVote, Is.EqualTo(EOperationAbortVote.Abort));
    Assert.That(result.Result, Is.EqualTo(EOperationResult.Error));
    
    Assert.That(sys.Key, Is.EqualTo((Constants.FinSystemName, LifecycleStage.Defaults.Write)));
    Assert.That(sys.Value, Is.EqualTo(SystemState.Create(Constants.FinSystemName, LifecycleStage.Defaults.Write).Completed(UtcDate.UtcNow)));
    Assert.That(obj.Key, Is.EqualTo((Constants.FinSystemName, LifecycleStage.Defaults.Write, Constants.CrmCustomer)));
    Assert.That(obj.Value, Is.EqualTo(ObjectState.Create(Constants.FinSystemName, LifecycleStage.Defaults.Write, Constants.CrmCustomer).Error(UtcDate.UtcNow, EOperationAbortVote.Abort, obj.Value.LastRunMessage ?? "", func.Thrown?.ToString())));
    Assert.That(allcusts, Is.Empty);
    Assert.That(maps, Is.Empty);
  }
}

public class TestingBatchWriteFunction : AbstractFunction<BatchWriteOperationConfig<CoreCustomer>, WriteOperationResult<CoreCustomer>> {

  private readonly WriteEntitiesToTargetSystem writer = new();
  public List<(CoreCustomer Core, EntityIntraSysMap.Created Map)> Created { get => writer.Created; set => writer.Created = value; }
  public List<(CoreCustomer Core, EntityIntraSysMap.Updated Map)> Updated { get => writer.Updated; set => writer.Updated = value; }
  public void Reset() => writer.Reset();
  public bool Throws { set => writer.Throws = value; }
  public Exception? Thrown { get => writer.Thrown; }
  
  public override FunctionConfig<BatchWriteOperationConfig<CoreCustomer>> Config { get; }

  public TestingBatchWriteFunction() {
    Config = new(Constants.FinSystemName, LifecycleStage.Defaults.Write, new ([
      new BatchWriteOperationConfig<CoreCustomer>(Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, writer)
    ]));
  }

  private class WriteEntitiesToTargetSystem : IWriteBatchEntitiesToTargetSystem<CoreCustomer> {

    internal List<(CoreCustomer Core, EntityIntraSysMap.Created Map)> Created { get; set; } = new();
    internal List<(CoreCustomer Core, EntityIntraSysMap.Updated Map)> Updated { get; set; } = new();
    internal bool Throws { get; set; }
    internal Exception? Thrown { get; private set; } 
    
    internal void Reset() {
      Created.Clear();
      Updated.Clear();
    }

    public Task<WriteOperationResult<CoreCustomer>> WriteEntities(BatchWriteOperationConfig<CoreCustomer> config, List<(CoreCustomer Core, EntityIntraSysMap.PendingCreate Map)> created, List<(CoreCustomer Core, EntityIntraSysMap.PendingUpdate Map)> updated) {
      if (Throws) throw Thrown = new Exception("mock function error");
      var news = created.Select(m => (m.Core, m.Map.SuccessCreate(m.Core.SourceId))).ToList();
      var updates = updated.Select(m => (m.Core, m.Map.SuccessUpdate())).ToList();
      Created.AddRange(news);
      Updated.AddRange(updates);
      return Task.FromResult<WriteOperationResult<CoreCustomer>>(new SuccessWriteOperationResult<CoreCustomer>(news, updates));
    }
  }

}