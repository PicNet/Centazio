using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Host;

public record HostSettings(string FunctionFilter, CentazioSettings CoreSettings, CentazioSecrets Secrets) {
  public List<string> ParseFunctionFilters() => FunctionFilter.Split([',', ';', '|', ' ']).Select(f => f.Trim()).Where(f => !String.IsNullOrEmpty(f)).ToList();
}

public class CentazioHost(HostSettings settings) {
  
  public Task Run() {
    Log.Logger = LogInitialiser
        .GetFileAndConsoleConfig()
        .CreateLogger();
    var types = GetCentazioFunctionTypess(settings.ParseFunctionFilters());
    Log.Information($"CentazioHost found functions:\n\t" + String.Join("\n\t", types.Select(type => type.Name)));
    var svcs = InitialiseDi(types); 
    var functions = InitialiseFunctions(svcs, types);
    return Task.CompletedTask;
  }

  private List<Type> GetCentazioFunctionTypess(List<string> filters) {
    return ReflectionUtils.GetAllTypesThatImplement(typeof(AbstractFunction<,>)).Where(type => MatchesFilter(type.FullName ?? String.Empty)).ToList();
    bool MatchesFilter(string name) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => name.Contains(filter, StringComparison.OrdinalIgnoreCase));
  }

  private ServiceProvider InitialiseDi(List<Type> functypes) {
    var svcs = new ServiceCollection();
    svcs.AddSingleton<CentazioSettings>();
    svcs.AddSingleton(typeof(IStagedEntityRepositoryFactory), GetFactoryType<IStagedEntityRepositoryFactory>(settings.CoreSettings.StagedEntityRepository.Provider));
    svcs.AddSingleton<IStagedEntityRepository>(prov => 
        prov.GetRequiredService<IStagedEntityRepositoryFactory>().GetRepository().GetAwaiter().GetResult());
    
    functypes.ForEach(type => svcs.AddSingleton(type));
    return svcs.BuildServiceProvider();
    
    // todo: instead of having all these hacks to support async Initialise methods, why not just call
    //    initialise on all repositorues at a later stage and leave DI to just manage construction not initialisation
    Type GetFactoryType<F>(string provider) {
      var potentials = ReflectionUtils.GetAllTypesThatImplement(typeof(F)); 
      return potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}]");
    }
    
  }

  // todo: add Runnable interface
  private List<object> InitialiseFunctions(ServiceProvider svcs, List<Type> types) {
    return types.Select(svcs.GetRequiredService).ToList();
  }

}
