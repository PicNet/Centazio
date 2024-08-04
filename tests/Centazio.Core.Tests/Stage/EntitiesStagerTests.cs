using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;
using Centazio.Test.Lib;
using static centazio.core.tests.DummySystems.Crm.Constants;

namespace centazio.core.tests.Stage;

public class EntitiesStagerTests {
  private TestingUtcDate dt;
  private IStagedEntityStore store;
  private EntitiesStager stager;
  
  [SetUp] public void SetUp() {
    dt = new TestingUtcDate();
    store = new InMemoryStagedEntityStore();
    stager = new EntitiesStager(store, dt);
  }

  [Test] public async Task Test_staging_a_single_record() {
    await stager.Stage(CrmSystemName, CrmCustomer, nameof(EntitiesStagerTests));
    
    var results1 = await store.Get(dt.Now.AddMilliseconds(-1), CrmSystemName, CrmCustomer);
    var results2 = await store.Get(dt.Now, CrmSystemName, CrmCustomer);
    
    var staged = results1.Single();
    Assert.That(staged, Is.EqualTo(new StagedEntity(CrmSystemName, CrmCustomer, dt.Now, nameof(EntitiesStagerTests))));
    Assert.That(results2, Is.Empty);
  }

  [Test] public async Task Test_staging_a_multiple_records() {
    var datas = Enumerable.Range(0, 10).Select(i => i.ToString());
    await stager.Stage(CrmSystemName, CrmCustomer, datas);
    
    var results1 = await store.Get(dt.Now.AddMicroseconds(-1), CrmSystemName, CrmCustomer);
    var results2 = await store.Get(dt.Now, CrmSystemName, CrmCustomer);
    
    var exp = Enumerable.Range(0, 10).Select(i => i.ToString()).Select(idx => new StagedEntity(CrmSystemName, CrmCustomer, dt.Now, idx.ToString())).ToList();
    Assert.That(results1, Is.EqualTo(exp));
    Assert.That(results2, Is.Empty);
  }
  
}