using System.Reflection;
using Centazio.Core.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Centazio.Core.Settings;

public interface ISettingsLoader {
  T Load<T>(string environment);
}

public class SettingsLoader(string filename = SettingsLoader.DEFAULT_FILE_NAME) : ISettingsLoader {

  private const string DEFAULT_FILE_NAME = "settings.json";
  
  private readonly string filename =  filename.EndsWith(".json") && !String.IsNullOrWhiteSpace(filename) ? filename : throw new Exception("settings filename should be a valid json file");
  
  public List<string> GetSettingsFilePathList(string environment) {
    var defaults = SearchForSettingsFile(filename.Replace(".json", $".defaults.json"));
    var basefile = SearchForSettingsFile(filename) ?? throw new Exception($"could not find settings file [{filename}] in the current directory hierarchy");
    var envfile = String.IsNullOrWhiteSpace(environment) ? null : SearchForSettingsFile(filename.Replace(".json", $".{environment}.json"));
    
    return new [] {defaults, basefile, envfile}.OfType<string>().ToList();
  }
  
  public T Load<T>(string environment) {
    var files = GetSettingsFilePathList(environment);
    Console.WriteLine($"loading setting files[{String.Join(',', files)}] environment[{environment}]");
    Log.Debug($"loading setting files[{String.Join(',', files)}] environment[{environment}]");
    
    var builder = new ConfigurationBuilder();
    files.ForEach(file => builder.AddJsonFile(file));
    
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
  
  public static TSettings RegisterSettingsHierarchy<TSettings>(TSettings settings, CentazioServicesRegistrar registrar) where TSettings : CentazioSettings => 
      RegisterSettingsHierarchyImpl(settings, registrar.Register);

  public static TSettings RegisterSettingsHierarchy<TSettings>(TSettings settings, IServiceCollection svcs) where TSettings : CentazioSettings => 
      RegisterSettingsHierarchyImpl(settings, (type, instance) => svcs.TryAdd(ServiceDescriptor.Singleton(type, instance)));

  private static TSettings RegisterSettingsHierarchyImpl<TSettings>(TSettings settings, Action<Type, object> adder) where TSettings : CentazioSettings {
    adder(typeof(TSettings), settings);
    typeof(TSettings).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
        .Where(pi => ReflectionUtils.IsRecord(pi.PropertyType) && pi.PropertyType != typeof(ServiceDescriptor))
        .Select(pi => { try { return pi.GetValue(settings); } catch { return null; } })
        .ForEach(v => { if (v is not null) adder(v.GetType(), v); });
    return settings;
  }
}