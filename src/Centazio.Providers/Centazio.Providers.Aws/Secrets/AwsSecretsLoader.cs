using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;

namespace Centazio.Providers.Aws.Secrets;

public delegate string EvaluateSecretIdForEnvironment(string environment);

public class AwsSecretsLoader(EvaluateSecretIdForEnvironment eval) : AbstractSecretsLoader {

  // todo: initialise client
  private readonly IAmazonSecretsManager client = null!;
  
  // todo: depending on the EvaluateSecretIdForEnvironment we could be
  //    loading the same sectrets store multiple times even if environment is different.
  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var id = eval(environment); 
    var res = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = id });
    if (String.IsNullOrEmpty(res.SecretString)) return required ? throw new Exception() : new Dictionary<string, string>();
    
    
    if (!res.SecretString.Trim().StartsWith("{")) return new Dictionary<string, string> { { id, res.SecretString } };

    var json = Json.Deserialize<Dictionary<string, string>>(res.SecretString);
    return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

}

public class AwsSecretsLoaderFactory() : IServiceFactory<ISecretsLoader> {
  // todo: how are we going to inject the EvaluateSecretIdForEnvironment delegate.  Use a simple template in the settings (use ITemplater?)
  public ISecretsLoader GetService() => new AwsSecretsLoader(env => env);
}
