using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core.Secrets;

namespace Centazio.Providers.Aws.Secrets;

public delegate string EvaluateSecretIdForEnvironment(string environment);

public class AwsSecretsLoader(EvaluateSecretIdForEnvironment eval) : AbstractSecretsLoader {

  // todo: initialise client
  private readonly IAmazonSecretsManager client = null!;
  
  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var id = eval(environment); 
    var res = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = id });
    if (String.IsNullOrEmpty(res.SecretString)) return required ? throw new Exception() : new Dictionary<string, string>();
    
    
    if (!res.SecretString.Trim().StartsWith("{")) return new Dictionary<string, string> { { id, res.SecretString } };

    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(res.SecretString);
    if (json is null) return new Dictionary<string, string>();
    return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

}