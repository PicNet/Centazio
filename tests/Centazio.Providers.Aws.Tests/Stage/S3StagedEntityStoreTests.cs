using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core.Stage;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;
using DotNet.Testcontainers.Builders;
using Testcontainers.Minio;

namespace Centazio.Providers.Aws.Tests.Stage;

public class S3StagedEntityStoreTests : StagedEntityStoreDefaultTests {

  private MinioContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    // var config = new MinioConfiguration(nameof(S3StagedEntityStoreTests), nameof(S3StagedEntityStoreTests));
    var network = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
    await network.CreateAsync();
    container = new MinioBuilder().WithNetwork(network).WithNetworkAliases("minio").Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    await container.StopAsync();
    await container.DisposeAsync();
  }
  
  protected override async Task<IStagedEntityStore> GetStore(int limit = 0) {
    // real client =  new AmazonS3Client(new BasicAWSCredentials(key, secret), new AmazonS3Config { RegionEndpoint = RegionEndpoint.APSoutheast2 });
    var config = new AmazonS3Config { ServiceURL = container.GetConnectionString(), ForcePathStyle = true };
    var client = new AmazonS3Client(container.GetAccessKey(), container.GetSecretKey(), config);
    return await new TestingS3StagedEntityStore(client, limit).Initalise();
  }

  class TestingS3StagedEntityStore(IAmazonS3 client, int limit = 100) : S3StagedEntityStore(client, BUCKET_NAME, limit) {
    
    private static readonly string BUCKET_NAME = nameof(TestingS3StagedEntityStore).ToLower(CultureInfo.InvariantCulture);

    public override async ValueTask DisposeAsync() {
      var objs = await Client.ListObjectsAsync(new ListObjectsRequest { BucketName = BUCKET_NAME });
      if (objs.S3Objects.Any()) {
        var response = await Client.DeleteObjectsAsync(new DeleteObjectsRequest { 
          BucketName = BUCKET_NAME, 
          Objects = objs.S3Objects.Select(s => new KeyVersion { Key = s.Key }).ToList() 
        });
        if (response.DeleteErrors.Any()) throw new Exception($"Deletion Errors:\n\t" + String.Join("\n\t", response.DeleteErrors));
      }
      await Client.DeleteBucketAsync(BUCKET_NAME);
      await base.DisposeAsync(); 
    }
  }
}

