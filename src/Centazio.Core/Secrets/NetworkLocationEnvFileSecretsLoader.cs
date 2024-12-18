using System.Reflection;
using Exception = System.Exception;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader  {
  T Load<T>();
}

public class NetworkLocationEnvFileSecretsLoader(string dir, string environment) : ISecretsLoader {
  private readonly string path = Path.Combine(dir, $"{environment}.env");
  public T Load<T>() {
    var secrets = LoadSecretsFileAsDictionary();
    return ValidateAndSetLoadedSecrets<T>(secrets); 
  }

  private Dictionary<string, string> LoadSecretsFileAsDictionary() => File.ReadAllLines(path)
      .Select(l => l.Split("#")[0].Trim())
      .Where(l => !String.IsNullOrEmpty(l))
      .Select(l => {
        var (Key, Value) = l.Split('=');
        return (key: Key, rest: Value);
      })
      .ToDictionary(kvp => kvp.key, kvp => String.Join('=', kvp.rest));

  private T ValidateAndSetLoadedSecrets<T>(Dictionary<string, string> secrets) {
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(typeof(T));
    var target = dtot ?? typeof(T);
    var typed = Activator.CreateInstance(target) ?? throw new Exception($"Type {target.FullName} could not be constructed");
    var missing = target.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).Where(p => {
      if (!secrets.ContainsKey(p.Name)) return true;

      p.SetValue(typed, Convert.ChangeType(secrets[p.Name], Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
      return false;
    }).ToList();
    if (missing.Any()) throw new Exception($"secrets file [{path}] has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return dtot is null ? (T) typed : ((IDto<T>)typed).ToBase();
  }

}