using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Centazio.Core.Settings;

public interface ISettingsLoader {
  T Load<T>(params List<string> environments);
}

public class SettingsLoader(string fnprefix = SettingsLoader.DEFAULT_FILE_NAME_PREFIX, string? dir = null) : ISettingsLoader {

  private const string DEFAULT_FILE_NAME_PREFIX = "settings";
  
  public List<string> GetSettingsFilePathList(params List<string> environments) {
    var potentials = new List<(string, bool)> { 
      ($"defaults/{fnprefix}.defaults.json", false),
      ($"defaults/{fnprefix}.<environment>.json", false),
      ($"{fnprefix}.json", true),
      ($"{fnprefix}.<environment>.json", false),
    };
    
    return potentials.SelectMany(p => {
      var (file, required) = p;
      var files = file.Contains("<environment>", StringComparison.Ordinal) ? 
          environments.Where(env => !String.IsNullOrWhiteSpace(env)).Select(env => file.Replace("<environment>", env, StringComparison.Ordinal)) : 
          [file];
      return files.Select(f => {
        var path = FsUtils.GetSolutionFilePath(dir is null ? [f] : [dir, f]);
        return File.Exists(path) ? path : !required ? null : throw new Exception($"could not find required settings file [{path}]");
      });
    }).OfType<string>().ToList();
  }
  
  public T Load<T>(params List<string> environments) {
    var files = GetSettingsFilePathList(environments);
    Log.Information($"loading setting files[{String.Join(',', files.Select(f => f.Split(Path.DirectorySeparatorChar).Last()))}] environments[{String.Join(',', environments)}]");
    
    var builder = new ConfigurationBuilder();
    // todo: handle comments - "//.*"?
    // todo: this is overriding whole sections, need custom merge, see: https://claude.ai/chat/e50dca2e-1d52-480f-bc3d-b028f60c934e
    files.ForEach(file => builder.AddJsonFile(file, false, true));
    
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(typeof(T));
    var obj = Activator.CreateInstance(dtot ?? typeof(T)) ?? throw new Exception($"Type {(dtot ?? typeof(T)).FullName} could not be constructed");
    var conf = builder.Build(); 
    conf.Bind(obj);
    return dtot is null ? (T) obj : ((IDto<T>)obj).ToBase();
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