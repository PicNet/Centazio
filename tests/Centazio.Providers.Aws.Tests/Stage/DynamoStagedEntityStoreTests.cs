using Amazon.DynamoDBv2.Model;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;

namespace Centazio.Providers.Aws.Tests.Stage;

public class DynamoStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit=0) {
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return await new TestingDynamoStagedEntityStore(secrets.AWS_KEY, secrets.AWS_SECRET, limit).Initalise();
  }
  
  class TestingDynamoStagedEntityStore(string key, string secret, int limit = 100) : DynamoStagedEntityStore(new DynamoStagedEntityStoreConfiguration(key, secret, TABLE_NAME, limit)) {
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

