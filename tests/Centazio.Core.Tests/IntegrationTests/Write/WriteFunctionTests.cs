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
    var results1 = (await funcrunner.RunFunction()).OpResults.Single().EntitiesWritten.ToList();
    var expresults1 = new [] { 
      (Core: customer1, Map: EntityIntraSystemMapping.CreatePending(customer1, Constants.CrmSystemName).SuccessCreate(customer1.SourceId) ), 
      (Core: customer2, Map: EntityIntraSystemMapping.CreatePending(customer2, Constants.CrmSystemName).SuccessCreate(customer2.SourceId) ) };
    var written1 = func.Written.ToList(); func.Written = new List<(CoreCustomer Core, EntityIntraSystemMapping Map)>();
    TestingUtcDate.DoTick();
    var customer22 = customer2 with { Checksum = Guid.NewGuid().ToString(), FirstName = "22", DateUpdated = UtcDate.UtcNow };
    var upsert2 = await core.Upsert(new [] { customer22 });
    var results2 = (await funcrunner.RunFunction()).OpResults.Single().EntitiesWritten.ToList();
    var expresults2 = new [] { (Core: customer22, Map: expresults1[1].Map.SuccessUpdate() ) };
    var written2 = func.Written.ToList(); func.Written = new List<(CoreCustomer Core, EntityIntraSystemMapping Map)>();

    Assert.That(upsert1, Is.EquivalentTo(new [] { customer1, customer2 }));
    Assert.That(results1, Is.EquivalentTo(expresults1));
    Assert.That(written1, Is.EquivalentTo(expresults1));
    
    Assert.That(upsert2, Is.EquivalentTo(new [] { customer22 }));
    Assert.That(results2, Is.EquivalentTo(expresults2));
    Assert.That(written2, Is.EquivalentTo(expresults2));
  }

}

public class TestingBatchWriteFunction : AbstractFunction<BatchWriteOperationConfig<CoreCustomer>, WriteOperationResult<CoreCustomer>> {

  private readonly WriteEntitiesToTargetSystem writer = new();
  public List<(CoreCustomer Core, EntityIntraSystemMapping Map)> Written { get => writer.Written; set => writer.Written = value; }
  
  public override FunctionConfig<BatchWriteOperationConfig<CoreCustomer>> Config { get; }

  public TestingBatchWriteFunction() {
    Config = new(Constants.CrmSystemName, Constants.Write, new ([
      new BatchWriteOperationConfig<CoreCustomer>(Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), writer)
    ]));
  }

  private class WriteEntitiesToTargetSystem : IWriteBatchEntitiesToTargetSystem<CoreCustomer> {

    internal List<(CoreCustomer Core, EntityIntraSystemMapping Map)> Written { get; set; } = new();

    public Task<WriteOperationResult<CoreCustomer>> WriteEntities(BatchWriteOperationConfig<CoreCustomer> config, List<(CoreCustomer Core, EntityIntraSystemMapping Map)> maps) {
      // todo: this is ugly make this code more error proof
      var updated = maps.Select(m => m with { Map = m.Map.Status == EEntityMappingStatus.Pending ? m.Map.SuccessCreate(m.Core.SourceId) : m.Map.SuccessUpdate() } ).ToList();
      Written.AddRange(updated);
      return Task.FromResult<WriteOperationResult<CoreCustomer>>(new SuccessWriteOperationResult<CoreCustomer>(updated));
    }

  }

}