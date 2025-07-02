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
    var typed = Activator.CreateInstance<T>();
    var missing = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty).Where(p => {
      if (!secrets.ContainsKey(p.Name)) return true;

      p.SetValue(typed, Convert.ChangeType(secrets[p.Name], Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
      return false;
    }).ToList();
    if (missing.Any()) throw new Exception($"secrets file has missing properties:\n\t{String.Join("\n\t", missing.Select(p => p.Name))}");
    return typed;
  }

}