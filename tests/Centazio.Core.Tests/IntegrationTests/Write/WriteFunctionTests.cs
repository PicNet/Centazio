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

}

public class TestingBatchWriteFunction : AbstractFunction<BatchWriteOperationConfig<CoreCustomer>, WriteOperationResult<CoreCustomer>> {

  private readonly WriteEntitiesToTargetSystem writer = new();
  public List<(CoreCustomer Core, EntityIntraSysMap.Created Map)> Created { get => writer.Created; set => writer.Created = value; }
  public List<(CoreCustomer Core, EntityIntraSysMap.Updated Map)> Updated { get => writer.Updated; set => writer.Updated = value; }
  public void Reset() => writer.Reset();
  
  public override FunctionConfig<BatchWriteOperationConfig<CoreCustomer>> Config { get; }

  public TestingBatchWriteFunction() {
    Config = new(Constants.FinSystemName, Constants.Write, new ([
      new BatchWriteOperationConfig<CoreCustomer>(Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), writer)
    ]));
  }

  private class WriteEntitiesToTargetSystem : IWriteBatchEntitiesToTargetSystem<CoreCustomer> {

    internal List<(CoreCustomer Core, EntityIntraSysMap.Created Map)> Created { get; set; } = new();
    internal List<(CoreCustomer Core, EntityIntraSysMap.Updated Map)> Updated { get; set; } = new();
    
    internal void Reset() {
      Created.Clear();
      Updated.Clear();
    }

    public Task<WriteOperationResult<CoreCustomer>> WriteEntities(BatchWriteOperationConfig<CoreCustomer> config, List<(CoreCustomer Core, EntityIntraSysMap.PendingCreate Map)> created, List<(CoreCustomer Core, EntityIntraSysMap.PendingUpdate Map)> updated) {
      var news = created.Select(m => (m.Core, m.Map.SuccessCreate(m.Core.SourceId))).ToList();
      var updates = updated.Select(m => (m.Core, m.Map.SuccessUpdate())).ToList();
      Created.AddRange(news);
      Updated.AddRange(updates);
      return Task.FromResult<WriteOperationResult<CoreCustomer>>(new SuccessWriteOperationResult<CoreCustomer>(news, updates));
    }
  }

}