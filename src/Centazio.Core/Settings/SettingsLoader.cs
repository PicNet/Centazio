using Microsoft.Extensions.Configuration;
using Serilog;

namespace Centazio.Core.Settings;

public interface ISettingsLoader<out T> where T : new() {
  T Load(string environment = "");
}

public class SettingsLoader<T>(string filename = SettingsLoader<T>.DEFAULT_FILE_NAME) : ISettingsLoader<T> where T : new() {

  private const string DEFAULT_FILE_NAME = "settings.json";
  
  public T Load(string environment = "") {
    if (!filename.EndsWith(".json")) throw new Exception("settings file should have a json extension");
    
    var basefile = SearchForSettingsFile(filename) ?? throw new Exception($"could not find settings file [{filename}] in the current directory hierarchy");
    var builder = new ConfigurationBuilder().AddJsonFile(basefile, false);
    var envfile = String.IsNullOrEmpty(environment) ? 
        null : 
        SearchForSettingsFile(filename.Replace(".json", $".{environment}.json"));
    if (envfile is not null) { builder.AddJsonFile(envfile, false); }
    
    var obj = new T();
    builder.Build().Bind(obj);
    return obj;
  }

  private string? SearchForSettingsFile(string file) {
    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) {
        Log.Debug("loading settings {Path}", path);
        return path;
      }
      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }
    return Impl(Environment.CurrentDirectory);
  }

}