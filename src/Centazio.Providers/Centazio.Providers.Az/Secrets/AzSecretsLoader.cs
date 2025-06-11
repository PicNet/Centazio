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
    
    if (settings.SecretsLoaderSettings.ProviderKey is not null && settings.SecretsLoaderSettings.ProviderSecret is not null)
    {
      Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", settings.SecretsLoaderSettings.ProviderKey);
      Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", settings.SecretsLoaderSettings.ProviderSecret);
    }
    
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = settings.AzureSettings.TenantId });

    return new SecretClient(new Uri($"https://{settings.AzureSettings.KeyVaultName}.vault.azure.net/"), credential);
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

  // todo WT: why do we have two `CentazioSettings`?
  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    if (settings.AzureSettings is null) throw new ArgumentNullException(nameof(settings.AzureSettings));

    return await CreateLoader(settings).Load<T>(environments.ToList());
  }

  private static AzSecretsLoader CreateLoader(CentazioSettings settings) => new(settings);

}