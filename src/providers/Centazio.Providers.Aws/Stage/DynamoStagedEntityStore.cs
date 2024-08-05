using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Centazio.Core;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Providers.Aws.Stage;

public class DynamoStagedEntityStore : AbstractStagedEntityStore, IDisposable {

  private readonly IAmazonDynamoDB client;

  public DynamoStagedEntityStore() {
    var (key, secret) = ("", "");
    client = new AmazonDynamoDBClient(
        new BasicAWSCredentials(key, secret), 
        new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });
  }

  public void Dispose() { client.Dispose(); }

  public async Task Initalise() {
    var tables = await client.ListTablesAsync();
    Console.WriteLine("Tables: " + JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true }));
    // var desc = await client.DescribeTableAsync("staged_entity");
    // Console.WriteLine(JsonSerializer.Serialize(desc, new JsonSerializerOptions { WriteIndented = true }));
  }

  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data) => throw new NotImplementedException();
  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) => throw new NotImplementedException();
  protected override Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) => throw new NotImplementedException();

}