namespace Centazio.Core.Secrets;

public class SecretsFileLoader(string dir) : AbstractSecretsLoader {

  
  
  protected override Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var path = GetSecretsFilePath(environment, required);
    if (String.IsNullOrWhiteSpace(path)) return Task.FromResult(new Dictionary<string, string>());
    
    return Task.FromResult(File.ReadAllLines(path)
        .Select(l => l.Split("#")[0].Trim())
        .Where(l => !String.IsNullOrEmpty(l))
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