using System.Reflection;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader<out T> where T : new() {
  T Load();
}

public class NetworkLocationEnvFileSecretsLoader<T>(string dir, string environment) : ISecretsLoader<T> where T : new() {
  public T Load() {
    var secrets = LoadSecretsFileAsDictionary(Path.Combine(dir, $"{environment}.env"));
    return ValidateAndSetLoadedSecrets(secrets); 
  }

  private Dictionary<string, string> LoadSecretsFileAsDictionary(string path) {
    return File.ReadAllLines(path)
        .Select(l => l.Split("#")[0].Trim())
        .Where(l => !String.IsNullOrEmpty(l))
        .Select(l => {
          var (Key, Value) = l.Split('=');
          return (key: Key, rest: Value);
        })
        .ToDictionary(kvp => kvp.key, kvp => String.Join('=', kvp.rest));
  }

  private T ValidateAndSetLoadedSecrets(Dictionary<string, string> secrets) {
    var typed = new T();
    var missing = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).Where(p => {
      if (!secrets.ContainsKey(p.Name)) return true;
      p.SetValue(typed, Convert.ChangeType(secrets[p.Name], p.PropertyType));
      return false;
    }).ToList();
    if (missing.Any()) throw new Exception($"secrets file has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return typed;
  }

}