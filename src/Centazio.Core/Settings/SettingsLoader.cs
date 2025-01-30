using System.Reflection;
using Centazio.Core.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Core.Settings;

public interface ISettingsLoader {
  T Load<T>(string environment);
}

public class SettingsLoader(string filename = SettingsLoader.DEFAULT_FILE_NAME) : ISettingsLoader {

  private const string DEFAULT_FILE_NAME = "settings.json";
  
  public T Load<T>(string environment) {
    if (!filename.EndsWith(".json")) throw new Exception("settings file should have a json extension");
    
    var basefile = SearchForSettingsFile(filename) ?? throw new Exception($"could not find settings file [{filename}] in the current directory hierarchy");
    Log.Debug($"loading settings - file[{basefile}] environment[{environment}]");
    
    var builder = new ConfigurationBuilder().AddJsonFile(basefile, false);
    var envfile = String.IsNullOrEmpty(environment) ? 
        null : 
        SearchForSettingsFile(filename.Replace(".json", $".{environment}.json"));
    if (envfile is not null) { builder.AddJsonFile(envfile, false); }
    
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(typeof(T));
    var obj = Activator.CreateInstance(dtot ?? typeof(T)) ?? throw new Exception($"Type {(dtot ?? typeof(T)).FullName} could not be constructed");
    builder.Build().Bind(obj);
    return dtot is null ? (T) obj : ((IDto<T>)obj).ToBase();
  }

  private string? SearchForSettingsFile(string file) {
    string? Impl(string dir) {
      var path = Path.Combine(dir, file);
      if (File.Exists(path)) return path;
      
      var parent = Directory.GetParent(dir)?.FullName;
      return parent is null ? null : Impl(parent);
    }
    return Impl(Environment.CurrentDirectory);
  }

  public static void RegisterSettingsAndRecordPropertiesAsSingletons<TSettings>(TSettings settings, CentazioHostServiceRegistrar svcs) where TSettings : CentazioSettings {
    svcs.Register(settings);
    typeof(TSettings).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
        .Where(pi => ReflectionUtils.IsRecord(pi.PropertyType) && pi.PropertyType != typeof(ServiceDescriptor))
        .Select(pi => { try { return pi.GetValue(settings); } catch { return null; } })
        .ForEach(v => { if (v is not null) svcs.Register(v.GetType(), v); });
  }

}