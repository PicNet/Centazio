using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Az.Secrets;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Az.Tests.Secrets;

public class AzSecretsLoaderTests: BaseSecretsLoaderTests {

  protected override async Task<ISecretsLoader> GetSecretsLoader() => 
      new AzSecretsLoaderFactory(await F.Settings()).GetService();

  // todo: refactor
  protected override async Task PrepareTestEnvironment(string environment, string contents) {
    var secrets = ParseSecretContents(contents);
    var settings = await F.Settings();
    var name = settings.AzureSettings.GetKeySecretNameForEnvironment(environment);
    var json = Json.Serialize(secrets);
    var client = GetSecretClient(settings);
  
    try {
      await client.SetSecretAsync(name, json);
    } catch (RequestFailedException) {
      await client.SetSecretAsync(new KeyVaultSecret(name, json));
    }
  }
  
  private Dictionary<string, object> ParseSecretContents(string contents) {
    var secrets = new Dictionary<string, object>();
  
    foreach (var line in contents.Split('\n')) {
      var trimmed = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
    
      var parts = trimmed.Split('=', 2);
      if (parts.Length == 2) secrets[parts[0]] = parts[1];
    }
  
    return secrets;
  }

  private SecretClient GetSecretClient(CentazioSettings settings) {
    var vaultUri = new Uri($"https://{settings.AzureSettings.KeyVaultName}.vault.azure.net/");
  
    return settings.SecretsLoaderSettings is { ProviderKey: not null, ProviderSecret: not null } ?
        new SecretClient(vaultUri, 
            new ClientSecretCredential(settings.AzureSettings.TenantId, settings.SecretsLoaderSettings.ProviderKey, settings.SecretsLoaderSettings.ProviderSecret)) :
        new SecretClient(vaultUri, new DefaultAzureCredential());
  }
  
}