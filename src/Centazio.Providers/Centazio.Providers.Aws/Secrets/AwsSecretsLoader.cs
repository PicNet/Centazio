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
  protected override List<string> FilterRedundantEnvironments(List<string> environments) => environments.DistinctBy(aws.GetSecretsStoreIdForEnvironment).ToList();

  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var id = aws.GetSecretsStoreIdForEnvironment(environment);
    var secretsstr = await GetSecretsValueString();
    if (secretsstr is null) return [];
    
    var json = Json.Deserialize<Dictionary<string, object>>(secretsstr);
    return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty);

    async Task<string?> GetSecretsValueString() {
      try {
        var str = (await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = id })).SecretString?.Trim();
        if (String.IsNullOrWhiteSpace(str) && required) throw new Exception($"secrets value in store [{id}] is empty.  This secrets store is required and should be available.");
        if (str is not null && !str.StartsWith("{")) throw new Exception($"secrets value is not a JSON object");
        return str;
      } catch (ResourceNotFoundException) {
        if (required) throw new Exception($"could not find the specified secrets store id [{id}] in the current aws account.  This secrets store is required and should be available.");
        return null;
      }
    }
  }

}

/// <summary>
///   When using Aws Secrets Manager the AwsSettings.SecretsMa.AwsSecretsLoader.LonagerStoreIdTemplate section is required in
///   the `settings.json` file.  This template string is used to get the Aws Store Id by replacing `&lt;environment&gt;`
///   with the required environment.
/// </summary>
/// <param name="settings">The `AwsSettings` section of the `settings.json` file.</param>
public class AwsSecretsLoaderFactory(CentazioSettings settings) :ISecretsFactory, IServiceFactory<ISecretsLoader> {

  public ISecretsLoader GetService() => new AwsSecretsLoader(settings.AwsSettings);
  
  // todo: why do we have two `CentazioSettings`?
  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    if (settings.AwsSettings is null) throw new ArgumentNullException(nameof(settings.AwsSettings));
    return await CreateLoader(settings.AwsSettings).Load<T>(environments.ToList());
  }

  private static AwsSecretsLoader CreateLoader(AwsSettings settings) => new(settings);
}