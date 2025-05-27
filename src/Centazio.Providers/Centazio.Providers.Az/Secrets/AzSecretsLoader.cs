using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Providers.Az.Secrets;

public class AzureSecretsLoader(AzureSettings azure) : AbstractSecretsLoader {

  private readonly SecretClient client = InitializeClient(azure);

  private static SecretClient InitializeClient(AzureSettings azure) {
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = azure.TenantId });

    return new SecretClient(new Uri($"https://{azure.KeyVaultName}.vault.azure.net/"), credential);
  }

  protected override List<string> FilterRedundantEnvironments(List<string> environments) => environments.DistinctBy(azure.GetKeySecretNameForEnvironment).ToList();

  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var name = azure.GetKeySecretNameForEnvironment(environment);

    try {
      var response = await client.GetSecretAsync(name);
      var secret = response?.Value;

      if (secret?.Value is null) return required ? throw new Exception($"Required secret '{name}' not found in Key Vault") : new Dictionary<string, string>();

      if (secret.Value.Trim().StartsWith("{")) {
        var json = Json.Deserialize<Dictionary<string, object>>(secret.Value);
        return json.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
      }

      return new Dictionary<string, string> { { name, secret.Value } };
    } catch (RequestFailedException ex) when (ex.Status == 404) {
      return required ? throw new Exception($"Required secret '{name}' not found in Key Vault") : new Dictionary<string, string>();
    }
  }

}

public class AzureSecretsLoaderFactory(AzureSettings azure) : IServiceFactory<ISecretsLoader> {

  public ISecretsLoader GetService() => new AzureSecretsLoader(azure);

  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    if (settings.AzureSettings is null) throw new ArgumentNullException(nameof(settings.AzureSettings));

    return await CreateLoader(settings.AzureSettings).Load<T>(environments.ToList());
  }

  private static AzureSecretsLoader CreateLoader(AzureSettings settings) => new(settings);

}