using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Promote;

public class PromotionStepsTests {

  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var steps = GetSteps(C.System1Name);
    var id = Guid.NewGuid();
    steps.bags = [
      new(null!) { SystemEntity = new System1Entity(id, "N1", "N1", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(null!) { SystemEntity = new System1Entity(id, "N2", "N2", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(null!) { SystemEntity = new System1Entity(id, "N3", "N3", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(null!) { SystemEntity = new System1Entity(Guid.NewGuid(), "N4", "N4", DateOnly.MinValue, TestingUtcDate.DoTick()) }
    ];
    
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    Assert.That(steps.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo(new [] { true, true, false, false }));
  }
  
  [Test] public void Test_IgnoreEntitiesBouncingBack() {
    var (steps1, steps2) = (GetSteps(C.System1Name), GetSteps(C.System2Name));
    steps1.bags = [
      new(null!) { UpdatedCoreEntity = new CoreEntity(new("1"), "1", "1", DateOnly.MinValue) },
      new(null!) { UpdatedCoreEntity = new CoreEntity2(new("3"), UtcDate.UtcNow) },
      new(null!) { UpdatedCoreEntity = new CoreEntity(new("2"), "2", "2", DateOnly.MinValue) },
      new(null!) { UpdatedCoreEntity = new CoreEntity2(new("4"), UtcDate.UtcNow) }
    ];
    steps2.bags = [
      new(null!) { UpdatedCoreEntity = new CoreEntity(new("1"), "1", "1", DateOnly.MinValue) },
      new(null!) { UpdatedCoreEntity = new CoreEntity2(new("3"), UtcDate.UtcNow) },
      new(null!) { UpdatedCoreEntity = new CoreEntity(new("2"), "2", "2", DateOnly.MinValue) },
      new(null!) { UpdatedCoreEntity = new CoreEntity2(new("4"), UtcDate.UtcNow) }
    ];
    
    steps1.IgnoreEntitiesBouncingBack(); steps2.IgnoreEntitiesBouncingBack();
    
    Assert.That(steps1.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo(new [] { false, true, false, true }));
    Assert.That(steps2.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo(new [] { true, false, true, false }));
  }

  
  private PromotionSteps GetSteps(SystemName system) {
    return new PromotionSteps(F.CoreRepo(), F.CtlRepo(), GetOpConfig(system));
  }
  
  private OperationStateAndConfig<PromoteOperationConfig> GetOpConfig(SystemName system) {
    var opconf = new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(system, LifecycleStage.Defaults.Promote, C.CoreEntityName),
        new BaseFunctionConfig(),
        new PromoteOperationConfig(typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, new PromoteOperationRunnerTests.ErrorPromoteEvaluator()), DateTime.MinValue);
    return opconf;
  }

}