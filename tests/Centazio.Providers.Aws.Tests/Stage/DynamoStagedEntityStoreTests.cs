﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Centazio.Core.Stage;
using Centazio.Core.Tests.Stage;
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

  protected override async Task<IStagedEntityStore> GetStore(int limit=0, Func<string, string>? checksum = null) {
    var client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = container.GetConnectionString() });
    return await new TestingDynamoStagedEntityStore(client, limit, checksum).Initalise();
    
  }

  class TestingDynamoStagedEntityStore(IAmazonDynamoDB client, int limit = 100, Func<string, string>? checksum = null) : DynamoStagedEntityStore(client, TABLE_NAME, limit, checksum ?? (s => s.GetHashCode().ToString())) {
    private const string TABLE_NAME = nameof(TestingDynamoStagedEntityStore);

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

