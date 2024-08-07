using System.Text.Json;

namespace Centazio.Core.Settings;

public interface ISettingsLoader<T> {
  T Load();
}

public class SettingsLoader<T>(string filename = SettingsLoader<T>.DEFAULT_FILE_NAME) : ISettingsLoader<T> {

  private const string DEFAULT_FILE_NAME = "settings.json";
  
  public T Load(string environment) {
    if (!filename.EndsWith(".json")) throw new Exception("settings file should have a json extension");
    var basefile = SearchForSettingsFile(filename) ?? throw new Exception($"could not find settings file [{filename}] in the current directory hierarchy");
    var obj = JsonSerializer.Deserialize<T>(File.ReadAllText(basefile)) ?? throw new Exception();
    var envfile =  SearchForSettingsFile(filename.Replace(".json", $".{environment}.json"));
    if (envfile != null) {
      var envobj = JsonSerializer.Deserialize<T>(File.ReadAllText(envfile)) ?? throw new Exception();
    }
    return obj;
  }

  private string? SearchForSettingsFile(string file) {
    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return path;
      var parent = Directory.GetParent(dir)?.FullName;
      return parent == null ? null : Impl(parent);
    }
    return Impl(Environment.CurrentDirectory);
  }

}