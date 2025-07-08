using Centazio.Core.CoreRepo;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Promote;

public class PromotionStepsTests {

  private readonly StagedEntity DUMMY = new(Guid.Empty, C.System1Name, C.SystemEntityName, UtcDate.UtcNow, new("{}"), new("{}"));
  
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var steps = GetSteps(C.System1Name);
    var id = Guid.NewGuid();
    steps.bags = [
      new(DUMMY, typeof(System1Entity)) { SystemEntity = new System1Entity(id, C.IgnoreCorrId, "N1", "N1", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(DUMMY, typeof(System1Entity)) { SystemEntity = new System1Entity(id, C.IgnoreCorrId, "N2", "N2", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(DUMMY, typeof(System1Entity)) { SystemEntity = new System1Entity(id, C.IgnoreCorrId, "N3", "N3", DateOnly.MinValue, TestingUtcDate.DoTick()) },
      new(DUMMY, typeof(System1Entity)) { SystemEntity = new System1Entity(Guid.NewGuid(), C.IgnoreCorrId, "N4", "N4", DateOnly.MinValue, TestingUtcDate.DoTick()) }
    ];
    
    steps.IgnoreUpdatesToSameEntityInBatch();
    
    Assert.That(steps.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo([true, true, false, false]));
  }
  
  [Test] public void Test_IgnoreEntitiesBouncingBack() {
    var (steps1, steps2) = (GetSteps(C.System1Name), GetSteps(C.System2Name));
    steps1.bags = [
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new("1"), new CoreEntity(new("1"), C.IgnoreCorrId, "1", "1", DateOnly.MinValue), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System2Name, C.SystemEntityName, new("3"), new CoreEntity2(new("3"), C.IgnoreCorrId, UtcDate.UtcNow), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new("2"), new CoreEntity(new("2"), C.IgnoreCorrId, "2", "2", DateOnly.MinValue), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System2Name, C.SystemEntityName, new("4"), new CoreEntity2(new("4"), C.IgnoreCorrId, UtcDate.UtcNow), Helpers.TestingCoreEntityChecksum) }
    ];
    steps2.bags = [
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new("1"), new CoreEntity(new("1"), C.IgnoreCorrId, "1", "1", DateOnly.MinValue), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System2Name, C.SystemEntityName, new("3"), new CoreEntity2(new("3"), C.IgnoreCorrId, UtcDate.UtcNow), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System1Name, C.SystemEntityName, new("2"), new CoreEntity(new("2"), C.IgnoreCorrId, "2", "2", DateOnly.MinValue), Helpers.TestingCoreEntityChecksum) },
      new(DUMMY, typeof(System1Entity)) { UpdatedCoreEntityAndMeta = CoreEntityAndMeta.Create(C.System2Name, C.SystemEntityName, new("4"), new CoreEntity2(new("4"), C.IgnoreCorrId, UtcDate.UtcNow), Helpers.TestingCoreEntityChecksum) }
    ];
    
    steps1.IgnoreEntitiesBouncingBack(); steps2.IgnoreEntitiesBouncingBack();
    
    Assert.That(steps1.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo([false, true, false, true]));
    Assert.That(steps2.bags.Select(bag => bag.IsIgnore), Is.EquivalentTo([true, false, true, false]));
  }

  
  private PromotionSteps GetSteps(SystemName system) {
    return new PromotionSteps(F.CoreRepo(), F.CtlRepo(), GetOpConfig(system));
  }
  
  private OperationStateAndConfig<PromoteOperationConfig> GetOpConfig(SystemName system) {
    var opconf = new OperationStateAndConfig<PromoteOperationConfig>(
        ObjectState.Create(system, LifecycleStage.Defaults.Promote, C.CoreEntityName, UtcDate.UtcNow),
        F.EmptyFunctionConfig(),
        new PromoteOperationConfig(system, typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, TestingDefaults.CRON_EVERY_SECOND, PromoteOperationRunnerTests.ErrorConvertingToCore), DateTime.MinValue);
    return opconf;
  }

}