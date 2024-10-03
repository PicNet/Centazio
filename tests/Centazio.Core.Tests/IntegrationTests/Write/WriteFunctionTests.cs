﻿using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;
using F = Centazio.Test.Lib.TestingFactories;

namespace Centazio.Core.Tests.IntegrationTests.Write;

public class WriteFunctionTests {
  [Test] public async Task Test_WriteFunction() {
    var (ctl, core, entitymap) = (F.CtlRepo(), F.CoreRepo(), F.CoreSysMap());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<WriteOperationConfig>(entitymap, core));
    var funcrunner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(func, oprunner, ctl);
    
    var customer1 = new CoreEntity(Guid.NewGuid().ToString(), "1", "1", new DateOnly(2000, 1, 1), UtcDate.UtcNow);
    var customer2 = new CoreEntity(Guid.NewGuid().ToString(), "2", "2", new DateOnly(2000, 1, 1), UtcDate.UtcNow);
    var upsert1 = await core.Upsert(Constants.CoreEntityName, [new (customer1, Helpers.TestingChecksum), new (customer2, Helpers.TestingChecksum)]);
    var res1 = (await funcrunner.RunFunction()).OpResults.Single();
    var expresults1 = new [] { 
      new CoreAndExternalMap(customer1, CoreToExternalMap.Create(customer1, Constants.System2Name).SuccessCreate(customer1.SourceId) ), 
      new CoreAndExternalMap(customer2, CoreToExternalMap.Create(customer2, Constants.System2Name).SuccessCreate(customer2.SourceId) ) };
    var (created1, updated1) = (func.Created.ToList(), func.Updated.ToList());
    func.Reset();
    
    TestingUtcDate.DoTick();
    
    var customer22 = customer2 with { FirstName = "22", DateUpdated = UtcDate.UtcNow };
    var upsert2 = await core.Upsert(Constants.CoreEntityName, [new(customer22, Helpers.TestingChecksum)]);
    var res2 = (await funcrunner.RunFunction()).OpResults.Single();
    var expresults2 = new [] { new CoreAndUpdatedMap(customer22, expresults1[1].Map.Update().SuccessUpdate() ) };
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
    var (ctl, core, entitymap) = (F.CtlRepo(), F.CoreRepo(), F.CoreSysMap());
    var (func, oprunner) = (new TestingBatchWriteFunction(), F.WriteRunner<WriteOperationConfig>(entitymap, core));
    func.Throws = true;
    var funcrunner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(func, oprunner, ctl);

    // add some data, as the write function will not be called if there is nothing to 'write'
    var entity = new CoreEntity(Guid.NewGuid().ToString(), "1", "1", new DateOnly(2000, 1, 1), UtcDate.UtcNow);
    await core.Upsert(Constants.CoreEntityName, [new (entity, Helpers.TestingChecksum)]);
    
    var result = (ErrorWriteOperationResult) (await funcrunner.RunFunction()).OpResults.Single();
    var sys = ctl.Systems.Single();
    var obj = ctl.Objects.Single();
    var allcusts = await core.Query<CoreEntity>(Constants.CoreEntityName, c => true);
    var maps = await entitymap.GetAll();

    Assert.That(result.EntitiesUpdated, Is.Empty);
    Assert.That(result.EntitiesCreated, Is.Empty);
    Assert.That(result.Exception, Is.EqualTo(func.Thrown));
    Assert.That(result.TotalChanges, Is.EqualTo(0));
    Assert.That(result.AbortVote, Is.EqualTo(EOperationAbortVote.Abort));
    Assert.That(result.Result, Is.EqualTo(EOperationResult.Error));
    
    Assert.That(sys.Key, Is.EqualTo((Constants.System2Name, LifecycleStage.Defaults.Write)));
    Assert.That(sys.Value, Is.EqualTo(SystemState.Create(Constants.System2Name, LifecycleStage.Defaults.Write).Completed(UtcDate.UtcNow)));
    Assert.That(obj.Key, Is.EqualTo((Constants.System2Name, LifecycleStage.Defaults.Write, Constants.CoreEntityName)));
    Assert.That(obj.Value, Is.EqualTo(ObjectState.Create(Constants.System2Name, LifecycleStage.Defaults.Write, Constants.CoreEntityName).Error(UtcDate.UtcNow, EOperationAbortVote.Abort, obj.Value.LastRunMessage ?? "", func.Thrown?.ToString())));
    Assert.That(allcusts, Is.EquivalentTo(new [] { entity }));
    Assert.That(maps, Is.Empty);
  }
}

public class TestingBatchWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, IWriteEntitiesToTargetSystem {

  public List<CoreAndExternalMap> Created { get; } = [];
  public List<CoreAndUpdatedMap> Updated { get; } = [];
  public bool Throws { get; set; }
  public Exception? Thrown { get; private set; }
  public override FunctionConfig<WriteOperationConfig> Config { get; }

  public TestingBatchWriteFunction() {
    Config = new FunctionConfig<WriteOperationConfig>(Constants.System2Name, LifecycleStage.Defaults.Write, [
      new(Constants.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, this)
    ]) { ThrowExceptions = false };
  }

  public void Reset() {
    Created.Clear();
    Updated.Clear();
  }

  public Task<WriteOperationResult> WriteEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> created, List<CoreAndPendingUpdateMap> updated) {
    if (Throws) throw Thrown = new Exception("mock function error");
    var news = created.Select(m => m.Created(m.Core.SourceId)).ToList();
    var updates = updated.Select(m => m.Updated()).ToList();
    Created.AddRange(news);
    Updated.AddRange(updates);
    return Task.FromResult<WriteOperationResult>(new SuccessWriteOperationResult(news, updates));
  }

}