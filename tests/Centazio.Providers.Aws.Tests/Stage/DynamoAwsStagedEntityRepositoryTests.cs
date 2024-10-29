using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.Aws.Stage;
using Centazio.Test.Lib.BaseProviderTests;
using Testcontainers.DynamoDb;

namespace Centazio.Providers.Aws.Tests.Stage;

public class DynamoAwsStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  
  private DynamoDbContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    container = new DynamoDbBuilder().Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    await container.StopAsync();
    await container.DisposeAsync();
  }

  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = container.GetConnectionString() });
    return await new TestingDynamoAwsStagedEntityRepository(client, limit, checksum).Initalise();
    
  }

  class TestingDynamoAwsStagedEntityRepository(IAmazonDynamoDB client, int limit, Func<string, StagedEntityChecksum> checksum) : DynamoAwsStagedEntityRepository(client, TABLE_NAME, limit, checksum) {
    private const string TABLE_NAME = nameof(TestingDynamoAwsStagedEntityRepository);

    public override async ValueTask DisposeAsync() {
      await Client.DeleteTableAsync(TABLE_NAME);
      while (true) {
        try { 
          await Client.DescribeTableAsync(TABLE_NAME);
          await Task.Delay(TimeSpan.FromMilliseconds(500));
        } catch (ResourceNotFoundException) { break; }
      }
      await base.DisposeAsync(); 
    }
  }
}

