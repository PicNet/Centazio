using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;

namespace Centazio.Core.Tests.Read;

public class PromoteOperationRunnerTests {

  private TestingStagedEntityStore staged;
  private TestingCtlRepository ctl;
  private TestingInMemoryCoreStorageRepository core;
  private IOperationRunner<PromoteOperationConfig<CoreCustomer>, PromoteOperationResult<CoreCustomer>> promoter;

  [SetUp] public void SetUp() {
    staged = new TestingStagedEntityStore();
    ctl = TestingFactories.CtlRepo();
    core = TestingFactories.CoreRepo();
    promoter = TestingFactories.PromoteRunner(staged, core);
    throw new Exception("todo implement test: " + promoter);
  }
  
  [TearDown] public async Task TearDown() {
    await staged.DisposeAsync();
    await ctl.DisposeAsync();
    await core.DisposeAsync();
  } 
}

public class PromoteOperationRunnerHelperExtensionsTests {
  [Test] public void Test_IgnoreMultipleUpdatesToSameEntity() {
    var id = Guid.NewGuid().ToString();
    var entities = new List<CoreCustomer> {
      TestingFactories.NewCoreCust("N1", "N1", id),
      TestingFactories.NewCoreCust("N2", "N2", id),
      TestingFactories.NewCoreCust("N3", "N3", id),
      TestingFactories.NewCoreCust("N4", "N4"),
    };
    
    var uniques = entities.IgnoreMultipleUpdatesToSameEntity();
    Assert.That(uniques, Is.EquivalentTo(new [] {entities[0], entities[3]}));
  }
  
  [Test] public async Task Test_IgnoreNonMeaninfulChanges() {
    var core = TestingFactories.CoreRepo();
    var entities1 = new List<CoreCustomer> {
      TestingFactories.NewCoreCust("N1", "N1", "1", "c1"),
      TestingFactories.NewCoreCust("N2", "N2", "2", "c2"),
      TestingFactories.NewCoreCust("N3", "N3", "3", "c3"),
      TestingFactories.NewCoreCust("N4", "N4", "4", "c4"),
    };
    await core.Upsert(entities1);
    
    var entities2 = new List<CoreCustomer> {
      TestingFactories.NewCoreCust("N12", "N12", "1", "c1"),
      TestingFactories.NewCoreCust("N22", "N22", "2", "c2"),
      TestingFactories.NewCoreCust("N32", "N32", "3", "c32"), // only this one gets updated as the checksum changed
      TestingFactories.NewCoreCust("N42", "N42", "4", "c4"),
    };
    // ideally these methods should be strongly typed using generics 
    var uniques = await entities2.IgnoreNonMeaninfulChanges(core);
    Assert.That(uniques, Is.EquivalentTo(new [] {entities2[2]}));
  }
  
  [Test] public void Test_IgnoreEntitiesBouncingBack() {
    Assert.Fail("todo: implement");
  }
}