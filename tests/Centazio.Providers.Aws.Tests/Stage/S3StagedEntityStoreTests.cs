using System.Globalization;
using Amazon.S3.Model;
using Centazio.Core.Entities.Ctl;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.Aws.Stage;

namespace Centazio.Providers.Aws.Tests.Stage;

public class S3StagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore() => await GetTestS3StagedEntityStore();

  private static async Task<IStagedEntityStore> GetTestS3StagedEntityStore(int pgsz=100) {
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return await new TestingS3StagedEntityStore(secrets.AWS_KEY, secrets.AWS_SECRET, pgsz).Initalise();
  }

  [Test] public async Task Test_get_returns_oldest_first_page_as_expected() {
    var pgsz = 10;
    var dynstore = await GetTestS3StagedEntityStore(pgsz);
    var start = dt.Now;
    var created = new List<StagedEntity>();
    foreach (var _ in Enumerable.Range(0, 25)) created.Add(await dynstore.Save(dt.Tick(), NAME, NAME, NAME));
    
    var exppage1 = created.Take(pgsz).ToList();
    var page1 = await dynstore.Get(start, NAME, NAME);
    
    var exppage2 = created.Skip(pgsz).Take(pgsz).ToList();
    var page2 = await dynstore.Get(exppage1.Last().DateStaged, NAME, NAME);
    
    var exppage3 = created.Skip(pgsz * 2).Take(pgsz).ToList();
    var page3 = await dynstore.Get(exppage2.Last().DateStaged, NAME, NAME);
    
    var page4 = await dynstore.Get(exppage3.Last().DateStaged, NAME, NAME);
    
    Assert.That(page1, Is.EquivalentTo(exppage1));
    Assert.That(page2, Is.EquivalentTo(exppage2));
    Assert.That(page3, Is.EquivalentTo(exppage3));
    Assert.That(page4, Is.Empty);
  }
  
  class TestingS3StagedEntityStore(string key, string secret, int pgsz = 100) : S3StagedEntityStore(new S3StagedEntityStoreConfiguration(key, secret, BUCKET_NAME, pgsz)) {
    
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

