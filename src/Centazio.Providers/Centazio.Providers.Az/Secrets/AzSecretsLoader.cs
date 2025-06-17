using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Providers.Az.Secrets;

public class AzSecretsLoader(CentazioSettings settings) : AbstractSecretsLoader {

  private readonly SecretClient client = InitializeClient(settings);

  private static SecretClient InitializeClient(CentazioSettings settings) {
    var vaultUri = new Uri($"https://{settings.AzureSettings.KeyVaultName}.vault.azure.net/");

    return settings.SecretsLoaderSettings is { ProviderKey: not null, ProviderSecret: not null } ?
        new SecretClient(vaultUri,
            new ClientSecretCredential(settings.AzureSettings.TenantId,
                settings.SecretsLoaderSettings.ProviderKey,
                settings.SecretsLoaderSettings.ProviderSecret)) :
        new SecretClient(vaultUri, new DefaultAzureCredential());
  }

  protected override List<string> FilterRedundantEnvironments(List<string> environments) => environments.DistinctBy(settings.AzureSettings.GetKeySecretNameForEnvironment).ToList();

  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var name = settings.AzureSettings.GetKeySecretNameForEnvironment(environment);

    try {
      var response = await client.GetSecretAsync(name);
      var secret = response?.Value;

      if (secret?.Value is null) return required ? throw new Exception($"Required secret '{name}' not found in Key Vault") : new Dictionary<string, string>();
      if (!secret.Value.Trim().StartsWith("{")) throw new Exception($"Secret value is not a JSON object");
      
      var json = Json.Deserialize<Dictionary<string, object>>(secret.Value);
      return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty);
        
    } catch (RequestFailedException ex) when (ex.Status == 404) {
      return required ? throw new Exception($"Required secret '{name}' not found in Key Vault") : new Dictionary<string, string>();
    }
  }

}

public class AzSecretsLoaderFactory(CentazioSettings settings) : IServiceFactory<ISecretsLoader> {

  public ISecretsLoader GetService() => new AzSecretsLoader(settings);

}