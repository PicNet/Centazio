using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Centazio.Core.Settings;

public interface ISettingsLoader {
  T Load<T>(params List<string> environments);
}

public record PotentialSettingFile(string FileName, bool Required, bool IsDefaultsFile);

public enum EDefaultSettingsMode {
  BOTH,
  ONLY_BASE_SETTINGS,
  ONLY_DEFAULT_SETTINGS
}

public record SettingsLoaderConfig(string? RootDirectory = null, EDefaultSettingsMode Defaults = EDefaultSettingsMode.BOTH) {
  
  public readonly string RootDirectory = RootDirectory ?? (Env.IsInDev() ? FsUtils.GetDevPath() : Environment.CurrentDirectory);
}

public class SettingsLoader(SettingsLoaderConfig? conf = null) : ISettingsLoader {
  private readonly SettingsLoaderConfig conf = conf ?? new SettingsLoaderConfig();
  
  public List<string> GetSettingsFilePathList(params List<string> environments) {
    var potentials = new List<PotentialSettingFile> {
      new (CentazioConstants.DEFAULTS_SETTINGS_FILE_NAME, true, true),
      new (CentazioConstants.ENV_SETTINGS_FILE_NAME, false, true),
      new (CentazioConstants.SETTINGS_FILE_NAME, true, false),
      new (CentazioConstants.ENV_SETTINGS_FILE_NAME, false, false),
    };
    
    return potentials.SelectMany(spec => {
      if (spec.IsDefaultsFile && conf.Defaults == EDefaultSettingsMode.ONLY_BASE_SETTINGS) return [];
      if (!spec.IsDefaultsFile && conf.Defaults == EDefaultSettingsMode.ONLY_DEFAULT_SETTINGS) return [];
      
      var files = spec.FileName.Contains("<environment>", StringComparison.Ordinal) ? 
          environments.Where(env => !String.IsNullOrWhiteSpace(env)).Select(env => spec.FileName.Replace("<environment>", env, StringComparison.Ordinal)) : 
          [spec.FileName];
      return files.Select(f => {
        var path = spec.IsDefaultsFile ? FsUtils.GetCliDir("defaults", f) : Path.Combine(conf.RootDirectory, f);
        return File.Exists(path) ? path : !spec.Required ? null : Throw();

        string Throw() {
          throw new Exception($"could not find required settings file [{path}]");
        }
      });
    }).OfType<string>().ToList();
  }
  
  public T Load<T>(params List<string> environments) {
    var files = GetSettingsFilePathList(environments);
    Log.Information($"loading setting files[{String.Join(',', files.Select(f => f.Split(Path.DirectorySeparatorChar).Last()))}] environments[{String.Join(',', environments)}]");
    
    var builder = new ConfigurationBuilder();
    files.ForEach(file => builder.AddJsonStream(Json.ReadFileAsStream(file)));
    
    var dtot = DtoHelpers.GetDtoTypeFromTypeHierarchy(typeof(T));
    var obj = Activator.CreateInstance(dtot ?? typeof(T)) ?? throw new Exception($"Type {(dtot ?? typeof(T)).FullName} could not be constructed");
    var settings = builder.Build(); 
    settings.Bind(obj);
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