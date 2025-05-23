using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;

namespace Centazio.Cli.Infra;

public static class SecretsManager {

  private static readonly Dictionary<ESecretsProviderType, ISecretsFactory> Providers = new() {
    [ESecretsProviderType.File] = new FileSecretsFactory(),
    [ESecretsProviderType.Aws] = new AwsSecretsFactory()
  };

  public static async Task<T> LoadSecrets<T>(CentazioSettings settings, params string[] environments)
  {
    var providerString = settings.SecretsLoaderSettings.Provider ?? "File";
    if (!Enum.TryParse<ESecretsProviderType>(providerString, out var provider))
      throw new ArgumentException($"Unknown secrets provider: {providerString}");

    if (!Providers.TryGetValue(provider, out var factory))
      throw new ArgumentException($"Provider {provider} is not implemented");

    return await factory.LoadSecrets<T>(settings, environments);
  }


}