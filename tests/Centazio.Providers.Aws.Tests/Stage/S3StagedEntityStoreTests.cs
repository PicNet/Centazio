using System.Globalization;
using Amazon.S3.Model;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;

namespace Centazio.Providers.Aws.Tests.Stage;

public class S3StagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit = 0) {
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return await new TestingS3StagedEntityStore(secrets.AWS_KEY, secrets.AWS_SECRET, limit).Initalise();
  }

  class TestingS3StagedEntityStore(string key, string secret, int limit = 100) : S3StagedEntityStore(key, secret, BUCKET_NAME, limit) {
    
    private static readonly string BUCKET_NAME = nameof(TestingS3StagedEntityStore).ToLower(CultureInfo.InvariantCulture);

    public override async ValueTask DisposeAsync() {
      var objs = await client.ListObjectsAsync(new ListObjectsRequest { BucketName = BUCKET_NAME });
      if (objs.S3Objects.Any()) {
        var response = await client.DeleteObjectsAsync(new DeleteObjectsRequest { 
          BucketName = BUCKET_NAME, 
          Objects = objs.S3Objects.Select(s => new KeyVersion { Key = s.Key }).ToList() 
        });
        if (response.DeleteErrors.Any()) throw new Exception($"Deletion Errors:\n\t" + String.Join("\n\t", response.DeleteErrors));
      }
      await client.DeleteBucketAsync(BUCKET_NAME);
      await base.DisposeAsync(); 
    }
  }
}

