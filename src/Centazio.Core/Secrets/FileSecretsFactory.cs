using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public class FileSecretsFactory : ISecretsFactory {

  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params string[] environments) {
    var loader = new SecretsFileLoader(settings.GetSecretsFolder());
    return await loader.Load<T>(environments.ToList());
  }

}