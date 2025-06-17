using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Aws.Tests.Secrets;
public class AwsSecretsLoaderTests : BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() =>
    new AwsSecretsLoaderFactory(await F.Settings()).GetService();
  
  // todo: refactor
  protected override async Task PrepareTestEnvironment(string environment, string contents) {
    var secrets = new Dictionary<string, object>();
    var settings = await F.Settings();
    
    foreach (var line in contents.Split('\n')) {
      var trimmed = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
        
      var parts = trimmed.Split('=', 2);
      if (parts.Length == 2) secrets[parts[0]] = parts[1];
    }
    
    var id = settings.AwsSettings.GetSecretsStoreIdForEnvironment(environment);
    var json = Json.Serialize(secrets);
    var client = new AmazonSecretsManagerClient(settings.AwsSettings.GetRegionEndpoint());
    
    try {
      await client.CreateSecretAsync(new CreateSecretRequest {
        Name = id,
        SecretString = json
      });
    } catch (ResourceExistsException) {
      await client.PutSecretValueAsync(new PutSecretValueRequest {
        SecretId = id,
        SecretString = json
      });
    }
  }


}