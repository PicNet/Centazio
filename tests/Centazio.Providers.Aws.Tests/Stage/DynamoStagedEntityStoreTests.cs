using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Centazio.Core.Stage;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;
using Testcontainers.DynamoDb;

namespace Centazio.Providers.Aws.Tests.Stage;

public class DynamoStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  
  private DynamoDbContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    container = new DynamoDbBuilder().Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    await container.StopAsync();
    await container.DisposeAsync();
  }

  protected override async Task<IStagedEntityStore> GetStore(int limit=0) => 
      await new TestingDynamoStagedEntityStore(new TestAmazonDynamoDBClientProvider(container), limit).Initalise();

  class TestAmazonDynamoDBClientProvider(DynamoDbContainer container) : IAmazonDynamoDBClientProvider {
    public IAmazonDynamoDB GetClient() => new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = container.GetConnectionString() });
  }
  
  class TestingDynamoStagedEntityStore(IAmazonDynamoDBClientProvider provider, int limit = 100) : DynamoStagedEntityStore(provider, TABLE_NAME, limit) {
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

