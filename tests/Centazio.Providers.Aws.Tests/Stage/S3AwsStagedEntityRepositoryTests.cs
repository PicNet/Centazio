﻿using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.Aws.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using DotNet.Testcontainers.Builders;
using Testcontainers.Minio;

namespace Centazio.Providers.Aws.Tests.Stage;

public class S3AwsStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {

  private MinioContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    var network = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();
    await network.CreateAsync();
    container = new MinioBuilder()
        .WithNetwork(network)
        .WithNetworkAliases("minio")
        .WithImage("quay.io/minio/minio")
        .Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    await container.StopAsync();
    await container.DisposeAsync();
  }
  
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var config = new AmazonS3Config { ServiceURL = container.GetConnectionString(), ForcePathStyle = true };
    var client = new AmazonS3Client(container.GetAccessKey(), container.GetSecretKey(), config);
    return await new TestingS3AwsStagedEntityRepository(client, limit).Initalise();
  }

  class TestingS3AwsStagedEntityRepository(IAmazonS3 client, int limit = 100) : S3AwsStagedEntityRepository(client, BUCKET_NAME, limit, Helpers.TestingStagedEntityChecksum) {
    
    private static readonly string BUCKET_NAME = nameof(TestingS3AwsStagedEntityRepository).ToLower(CultureInfo.InvariantCulture);

    public override async ValueTask DisposeAsync() {
      var objs = await Client.ListObjectsAsync(new ListObjectsRequest { BucketName = BUCKET_NAME });
      if (objs.S3Objects is not null && objs.S3Objects.Any()) {
        var response = await Client.DeleteObjectsAsync(new DeleteObjectsRequest { 
          BucketName = BUCKET_NAME, 
          Objects = objs.S3Objects.Select(s => new KeyVersion { Key = s.Key }).ToList() 
        });
        if (response.DeleteErrors is not null && response.DeleteErrors.Any()) throw new Exception($"Deletion Errors:\n\t" + String.Join("\n\t", response.DeleteErrors));
      }
      await Client.DeleteBucketAsync(BUCKET_NAME);
      await base.DisposeAsync(); 
    }
  }
}

