using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public class SecretsFileLoader(string dir) : AbstractSecretsLoader {

  
  
  protected override Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var path = GetSecretsFilePath(environment, required);
    if (String.IsNullOrWhiteSpace(path)) return Task.FromResult(new Dictionary<string, string>());
    
    return Task.FromResult(File.ReadAllLines(path)
        .Select(l => l.Trim())
        .Where(l => !String.IsNullOrEmpty(l) && !l.StartsWith('#'))
        .Select(l => {
          var (Key, Value) = l.Split('=');
          return (key: Key.Trim(), value: String.Join('=', Value).Trim());
        })
        .Where(kvp => !String.IsNullOrWhiteSpace(kvp.key) && !String.IsNullOrWhiteSpace(kvp.value))
        .ToDictionary(kvp => kvp.key, kvp => kvp.value));
  }

  public string? GetSecretsFilePath(string environment, bool required) {
    var path = Path.Combine(dir, $"{environment}.env");
    return File.Exists(path) ? path : !required ? null : throw new FileNotFoundException(path);
  }
}

public class FileSecretsLoaderFactory(string dir) :ISecretsFactory, IServiceFactory<ISecretsLoader> {

  public ISecretsLoader GetService() => new SecretsFileLoader(dir);
  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params string[] environments)
  {
    if (settings.SecretsFolders == null) throw new ArgumentNullException(nameof(settings.SecretsFolders));
    return await CreateLoader(settings.GetSecretsFolder()).Load<T>(environments.ToList());
  }

  private static SecretsFileLoader CreateLoader(string dir) => new(dir);
}