using Amazon.DynamoDBv2.Model;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;

namespace Centazio.Providers.Aws.Tests.Stage;

public class DynamoStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore() => await GetTestDynamoStagedEntityStore();

  private static async Task<IStagedEntityStore> GetTestDynamoStagedEntityStore(int pgsz=100) {
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return await new TestingDynamoStagedEntityStore(secrets.AWS_KEY, secrets.AWS_SECRET, pgsz).Initalise();
  }

  [Test] public async Task Test_get_returns_oldest_first_page_as_expected() {
    var pgsz = 10;
    var dynstore = await GetTestDynamoStagedEntityStore(pgsz);
    var start = dt.Now;
    var created = new List<StagedEntity>();
    foreach (var _ in Enumerable.Range(0, 25)) created.Add(await dynstore.Save(dt.Tick(), NAME, NAME, NAME));
    
    var exppage1 = created.Take(pgsz).ToList();
    var page1 = await dynstore.Get(start, NAME, NAME);
    
    var exppage2 = created.Skip(pgsz).Take(pgsz).ToList();
    var page2 = await dynstore.Get(exppage1.Last().DateStaged, NAME, NAME);
    
    var exppage3 = created.Skip(pgsz * 2).Take(pgsz).ToList();
    var page3 = await dynstore.Get(exppage2.Last().DateStaged, NAME, NAME);
    
    var page4 = await dynstore.Get(exppage3.Last().DateStaged, NAME, NAME);
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  class TestingDynamoStagedEntityStore(string key, string secret, int pgsz = 100) : DynamoStagedEntityStore(new DynamoStagedEntityStoreConfiguration(key, secret, TABLE_NAME, pgsz)) {
    private const string TABLE_NAME = nameof(TestingDynamoStagedEntityStore);

    public override async ValueTask DisposeAsync() {
      await client.DeleteTableAsync(TABLE_NAME);
      while (true) {
        try { 
          await client.DescribeTableAsync(TABLE_NAME);
          await Task.Delay(TimeSpan.FromMilliseconds(500));
        } catch (ResourceNotFoundException) { break; }
      }
      await base.DisposeAsync(); 
    }
  }
}

