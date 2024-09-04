using System.Reflection;
using Exception = System.Exception;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader<out T>  {
  T Load();
}

public class NetworkLocationEnvFileSecretsLoader<T>(string dir, string environment) : ISecretsLoader<T> where T : new() {
  private readonly string path = Path.Combine(dir, $"{environment}.env");
  public T Load() {
    var secrets = LoadSecretsFileAsDictionary();
    return ValidateAndSetLoadedSecrets(secrets); 
  }

  private Dictionary<string, string> LoadSecretsFileAsDictionary() => File.ReadAllLines(path)
      .Select(l => l.Split("#")[0].Trim())
      .Where(l => !String.IsNullOrEmpty(l))
      .Select(l => {
        var (Key, Value) = l.Split('=');
        return (key: Key, rest: Value);
      })
      .ToDictionary(kvp => kvp.key, kvp => String.Join('=', kvp.rest));

  private T ValidateAndSetLoadedSecrets(Dictionary<string, string> secrets) {
    var typed = new T();
    var missing = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).Where(p => {
      if (!secrets.ContainsKey(p.Name)) return true;

      p.SetValue(typed, Convert.ChangeType(secrets[p.Name], Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
      return false;
    }).ToList();
    if (missing.Any()) throw new Exception($"secrets file [{path}] has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return typed;
  }

}