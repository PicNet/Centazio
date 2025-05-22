using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Providers.Aws.Secrets;

public class AwsSecretsLoader(AwsSettings aws) : AbstractSecretsLoader {
  
  private readonly IAmazonSecretsManager client = InitializeClient(aws);
  
  private static IAmazonSecretsManager InitializeClient(AwsSettings aws) {
    
    Environment.SetEnvironmentVariable("AWS_PROFILE", aws.AccountName);
    Environment.SetEnvironmentVariable("AWS_SDK_LOAD_CONFIG", "1");
    
    return new AmazonSecretsManagerClient(aws.GetRegionEndpoint());
  }
  
  // remove redundant environments, i.e. environments that result in the same Secret Store Id 
  protected override List<string> FilterRedundantEnvironments(List<string> environments) => 
      environments.DistinctBy(aws.GetSecretsStoreIdForEnvironment).ToList();

  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var id = aws.GetSecretsStoreIdForEnvironment(environment); 
    var res = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = id });
    if (String.IsNullOrEmpty(res.SecretString)) return required ? throw new Exception() : new Dictionary<string, string>();
    
    
    if (!res.SecretString.Trim().StartsWith("{")) return new Dictionary<string, string> { { id, res.SecretString } };

    var json = Json.Deserialize<Dictionary<string, object>>(res.SecretString);
    return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? String.Empty);
  }

}

/// <summary>
/// When using Aws Secrets Manager the AwsSettings.SecretsManagerStoreIdTemplate section is required in
/// the `settings.json` file.  This template string is used to get the Aws Store Id by replacing `&lt;environment&gt;`
/// with the required environment.
/// </summary>
/// <param name="aws">The `AwsSettings` section of the `settings.json` file.</param>
public class AwsSecretsLoaderFactory(AwsSettings aws) : IServiceFactory<ISecretsLoader> {
  public ISecretsLoader GetService() => new AwsSecretsLoader(aws);
}
