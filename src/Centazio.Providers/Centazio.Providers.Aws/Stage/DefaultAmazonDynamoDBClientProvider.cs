using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace Centazio.Providers.Aws.Stage;

public interface IAmazonDynamoDBClientProvider {
  IAmazonDynamoDB GetClient();
}

public class DefaultAmazonDynamoDBClientProvider(string key, string secret) : IAmazonDynamoDBClientProvider {
  public IAmazonDynamoDB GetClient() => new AmazonDynamoDBClient(
      new BasicAWSCredentials(key, secret), 
      new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });

}