using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core.Secrets;

namespace Centazio.Providers.Aws.Secrets;

public class AwsSecretsLoader(string secretid) : AbstractSecretsLoader {

  // todo: initialise client
  private readonly IAmazonSecretsManager client = null!;
  
  // todo: handle loading multiple environments and supporting override - see `SecretsFileLoader`
  protected override async Task<IDictionary<string, string>> LoadSecretsAsDictionary(List<string> environments) {
    var res = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretid });
    if (string.IsNullOrEmpty(res.SecretString)) { throw new Exception(); }
    if (!res.SecretString.Trim().StartsWith("{")) return new Dictionary<string, string> { { secretid, res.SecretString } };

    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(res.SecretString);
    if (json is null) return new Dictionary<string, string>();
    return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

  }

}