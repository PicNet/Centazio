using System.Reflection;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader<T> {
  T Load();
}

public class NetworkLocationEnvFileSecretsLoader<T>(string dir, string environment) : ISecretsLoader<T> where T : new() {
  public T Load() {
    var file = Path.Combine(dir, $"{environment}.env");
    if (!File.Exists(file)) throw new Exception($"could not find the specified secrets file [{file}]");
    var secrets = File.ReadAllLines(file)
        .Select(l => l.Split("#")[0].Trim())
        .Where(l => !String.IsNullOrEmpty(l))
        .Select(l => l.Split('='))
        .ToDictionary(kvp => kvp[0].Trim(), kvp => kvp[1].Trim());
    var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).ToList();
    var missing = props.Where(p => !secrets.ContainsKey(p.Name)).ToList();
    if (missing.Any()) throw new Exception($"secrets file [{file}] does not have the following expected keys [{String.Join(", ", missing.Select(m => m.Name))}]");
    
    var typed = new T();
    props.ForEach(p => p.SetValue(typed, secrets[p.Name]));
    return typed;
  }
}