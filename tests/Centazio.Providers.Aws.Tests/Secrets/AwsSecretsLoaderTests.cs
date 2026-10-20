using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Aws.Tests.Secrets;

// todo: these tests should clean up after themselves and not leave testing buckets in AWS
public class AwsSecretsLoaderTests : BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() =>
    new AwsSecretsLoaderFactory(await F.Settings()).GetService();
  
  protected override async Task PrepareTestEnvironment(string environment, Dictionary<string, string> secrets) {
    var (settings, json) = (await F.Settings(), Json.Serialize(secrets));
    var (id, client) = (settings.AwsSettings.GetSecretsStoreIdForEnvironment(environment), new AmazonSecretsManagerClient(settings.AwsSettings.GetRegionEndpoint()));
    
    // update first so CI only needs PutSecretValue on the existing testing secrets
    try {
      await client.PutSecretValueAsync(new PutSecretValueRequest { SecretId = id, SecretString = json });
    } catch (ResourceNotFoundException) {
      await client.CreateSecretAsync(new CreateSecretRequest { Name = id, SecretString = json });
    }
  }


}