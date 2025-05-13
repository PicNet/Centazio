namespace Centazio.Core.Secrets;

public class SecretsFileLoader(string dir) : AbstractSecretsLoader {

  protected override Task<IDictionary<string, string>> LoadSecretsAsDictionary(List<string> environments) {
    if (!environments.Any()) throw new ArgumentNullException(nameof(environments));
    var paths = environments.Select((env, idx) => GetSecretsFilePath(env, idx == 0)).OfType<string>().ToList();
    Log.Information($"loading secrets files[{String.Join(',', paths.Select(f => f.Split(Path.DirectorySeparatorChar).Last()))}] environments[{String.Join(',', environments)}]");
    return Task.FromResult<IDictionary<string, string>>(paths
        .Select(LoadFileAsDictionary)
        .Aggregate(new Dictionary<string, string>(), (secrets, step) => {
      step.ForEach(kvp => secrets[kvp.Key] = kvp.Value);
      return secrets;
    }));
    
    Dictionary<string, string> LoadFileAsDictionary(string path) => File.ReadAllLines(path)
        .Select(l => l.Split("#")[0].Trim())
        .Where(l => !String.IsNullOrEmpty(l))
        .Select(l => {
          var (Key, Value) = l.Split('=');
          return (key: Key.Trim(), value: String.Join('=', Value).Trim());
        })
        .Where(kvp => !String.IsNullOrWhiteSpace(kvp.key) && !String.IsNullOrWhiteSpace(kvp.value))
        .ToDictionary(kvp => kvp.key, kvp => kvp.value);
  }

  public string? GetSecretsFilePath(string environment, bool required) {
    var path = Path.Combine(dir, $"{environment}.env");
    return File.Exists(path) ? path : !required ? null : throw new FileNotFoundException(path);
  }
}