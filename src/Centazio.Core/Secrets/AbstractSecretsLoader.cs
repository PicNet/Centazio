namespace Centazio.Core.Secrets;

public abstract class AbstractSecretsLoader : ISecretsLoader {

  public async Task<T> Load<T>(params List<string> environments) {
    var envs = FilterRedundantEnvironments(environments); 
    var dict = await LoadSecretsAsDictionary(envs);
    return ValidateAndConvertSecretsToDto<T>(dict);
  }

  // override if some environments can be ignored for performance
  protected virtual List<string> FilterRedundantEnvironments(List<string> environments) { return environments; }

  private async Task<Dictionary<string, string>> LoadSecretsAsDictionary(List<string> environments) {
    if (!environments.Any()) throw new ArgumentNullException(nameof(environments));
    
    Log.Information($"loading secrets environments[{String.Join(',', environments.Select(f => f.Split(Path.DirectorySeparatorChar).Last()))}]");
    return (await environments
        .Select((env, idx) => LoadSecretsAsDictionaryForEnvironment(env, idx == 0))
        .Synchronous())
        .Aggregate(new Dictionary<string, string>(), (secrets, step) => {
      step.ForEach(kvp => secrets[kvp.Key] = kvp.Value);
      return secrets;
    });
  }
  
  protected abstract Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required);
  

  private T ValidateAndConvertSecretsToDto<T>(Dictionary<string, string> secrets) {
    var trimmed = secrets
        .Where(kvp => !String.IsNullOrWhiteSpace(kvp.Key) && !String.IsNullOrWhiteSpace(kvp.Value))
        .Select(kvp => new KeyValuePair<string,string>(kvp.Key.Trim(), kvp.Value.Trim()))
        .ToDictionary();
    var typed = Activator.CreateInstance(typeof(T)) ?? throw new Exception($"Type {typeof(T).FullName} could not be constructed");
    var missing = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).Where(p => {
      if (!trimmed.ContainsKey(p.Name)) return !ReflectionUtils.IsNullable(p);
      p.SetValue(typed, Convert.ChangeType(trimmed[p.Name], Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
      return false;
    }).ToList();
    if (missing.Any()) throw new Exception($"secrets file has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return (T) typed;
  }

}