using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public class FileSecretsLoader(CentazioSettings settings) : AbstractSecretsLoader {
  
  protected override async Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) {
    var path = GetSecretsFilePath(environment, required);
    if (String.IsNullOrWhiteSpace(path)) return new Dictionary<string, string>();
    
    return SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(await File.ReadAllTextAsync(path));
  }

  public string? GetSecretsFilePath(string environment, bool required) {
    var path = Path.Combine(settings.GetSecretsFolder(), $"{environment}.env");
    return File.Exists(path) ? path : !required ? null : throw new FileNotFoundException(path);
  }
}

public class FileSecretsLoaderFactory(CentazioSettings settings) : IServiceFactory<ISecretsLoader> {
  public ISecretsLoader GetService() => new FileSecretsLoader(settings);
}

public static class SecretsLoaderUtils {
  public static Dictionary<string, string> SplitFlatContentIntoSecretsDict(string contents) => 
      contents.Split('\n')
          .Select(l => l.Trim().Split('='))
          .Where(tokens => tokens.Length > 1 && !String.IsNullOrWhiteSpace(tokens[0]) && !tokens[0].StartsWith('#'))
          .Select(tokens => (key: tokens[0].Trim(), value: String.Join('=', tokens.Skip(1)).Trim()))
          .Where(kvp => !String.IsNullOrWhiteSpace(kvp.value))
          .ToDictionary(kvp => kvp.key, kvp => kvp.value);

}