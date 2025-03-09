using Exception = System.Exception;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader  {
  string GetSecretsFilePath(string environment);
  T Load<T>(string environment);
}

public class SecretsFileLoader(string dir) : ISecretsLoader {
  
  public string GetSecretsFilePath(string environment) {
    var path = Path.Combine(dir, $"{environment}.env");
    if (!File.Exists(path)) throw new FileNotFoundException(path);
    return path;
  }
  
  public T Load<T>(string environment) {
    var path = GetSecretsFilePath(environment);
    Log.Information($"loading secrets - file [{path.Split(Path.DirectorySeparatorChar).Last()}]");
    
    var secrets = LoadSecretsFileAsDictionary(path);
    return ValidateAndSetLoadedSecrets<T>(secrets); 
  }

  private Dictionary<string, string> LoadSecretsFileAsDictionary(string path) => File.ReadAllLines(path)
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
    if (missing.Any()) throw new Exception($"secrets file has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return dtot is null ? (T) typed : ((IDto<T>)typed).ToBase();
  }
}