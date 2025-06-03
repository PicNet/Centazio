using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public class EnvironmentSecretsLoader : AbstractSecretsLoader {

  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    try {
      var vars = Environment.GetEnvironmentVariables();
      var lscrts = new Dictionary<string, string>();
      foreach (var key in vars.Keys) {
        var k = key.ToString();
        if (k is null || !k.StartsWith(environment.ToUpper() + "_")) continue;

        var fieldName = k.Substring(environment.Length + 1);
        lscrts[fieldName] = vars[key]?.ToString() ?? string.Empty;
      }

      return await Task.FromResult(lscrts);
    }
    catch (Exception ex) {
      throw new Exception($"Error loading secrets as dictionary for environment '{environment}': {ex.Message}", ex);
    }
  }

}

public class EnvironmentVariableSecretsLoaderFactory : ISecretsFactory, IServiceFactory<ISecretsLoader> {
  public ISecretsLoader GetService() => new EnvironmentSecretsLoader();
  
  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments) {
    return await new EnvironmentSecretsLoader().Load<T>(environments.ToList());
  }
}
