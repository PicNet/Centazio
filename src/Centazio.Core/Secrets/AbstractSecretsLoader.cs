namespace Centazio.Core.Secrets;

public abstract class AbstractSecretsLoader : ISecretsLoader {

  public async Task<T> Load<T>(params List<string> environments) {
    var dict = await LoadSecretsAsDictionary(environments);
    return ValidateAndConvertSecretsToDto<T>(dict);
  }

  private async Task<IDictionary<string, string>> LoadSecretsAsDictionary(List<string> environments) {
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
  

  private T ValidateAndConvertSecretsToDto<T>(IDictionary<string, string> secrets) {
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