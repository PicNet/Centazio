using Exception = System.Exception;

namespace Centazio.Core.Secrets;

public interface ISecretsLoader  {
  T Load<T>(params List<string> environments);
  string? GetSecretsFilePath(string environment, bool required);
}

public class SecretsFileLoader(string dir) : ISecretsLoader {

  public T Load<T>(params List<string> environments) {
    if (!environments.Any()) throw new ArgumentNullException(nameof(environments));
    var paths = environments.Select((env, idx) => GetSecretsFilePath(env, idx == 0)).OfType<string>().ToList();
    Log.Information($"loading secrets files[{String.Join(',', paths.Select(f => f.Split(Path.DirectorySeparatorChar).Last()))}] environments[{String.Join(',', environments)}]");
    var secrets = new Dictionary<string, string>();
    paths.ForEach(path => 
        LoadSecretsFileAsDictionary(path)
            .Where(kvp => !String.IsNullOrWhiteSpace(kvp.Value))
            .ForEach(kvp => secrets[kvp.Key] = kvp.Value));
    return ValidateAndSetLoadedSecrets<T>(secrets); 
  }

  public string? GetSecretsFilePath(string environment, bool required) {
    var path = Path.Combine(dir, $"{environment}.env");
    return File.Exists(path) ? path : !required ? null : throw new FileNotFoundException(path);
  }

  private Dictionary<string, string> LoadSecretsFileAsDictionary(string path) => File.ReadAllLines(path)
      .Select(l => l.Split("#")[0].Trim())
      .Where(l => !String.IsNullOrEmpty(l))
      .Select(l => {
        var (Key, Value) = l.Split('=');
        return (key: Key, rest: Value);
      })
      .ToDictionary(kvp => kvp.key, kvp => String.Join('=', kvp.rest).Trim());

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