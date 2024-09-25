using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core.Stage;
using Centazio.Core.Tests.Stage;
using Centazio.Providers.Aws.Stage;
using Centazio.Test.Lib;
using DotNet.Testcontainers.Builders;
using Testcontainers.Minio;

namespace Centazio.Providers.Aws.Tests.Stage;

public class S3StagedEntityStoreTests : StagedEntityStoreDefaultTests {

  private MinioContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    var network = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
    await network.CreateAsync();
    container = new MinioBuilder().WithNetwork(network).WithNetworkAliases("minio").Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    await container.StopAsync();
    await container.DisposeAsync();
  }
  
  protected override async Task<IStagedEntityStore> GetStore(int limit = 0, Func<string, string>? checksum = null) {
    var config = new AmazonS3Config { ServiceURL = container.GetConnectionString(), ForcePathStyle = true };
    var client = new AmazonS3Client(container.GetAccessKey(), container.GetSecretKey(), config);
    return await new TestingS3StagedEntityStore(client, limit).Initalise();
  }

  class TestingS3StagedEntityStore(IAmazonS3 client, int limit = 100) : S3StagedEntityStore(client, BUCKET_NAME, limit, Helpers.TestingChecksum) {
    
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

