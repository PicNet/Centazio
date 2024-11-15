using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Host;

public class HostBootstrapper(CentazioSettings settings) {

  public async Task<List<IRunnableFunction>> InitHost(List<string> filters) {
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig().CreateLogger();
    var types = GetCentazioFunctionTypess(filters);
    Log.Information($"CentazioHost found functions:\n\t" + String.Join("\n\t", types.Select(type => type.Name)));
    var prov = InitialiseDi(types);
    await InitialiseCoreServices(prov);
    return InitialiseFunctions(prov, types);
  }

  private List<Type> GetCentazioFunctionTypess(List<string> filters) {
    return ReflectionUtils.GetAllTypesThatImplement(typeof(AbstractFunction<>), settings.AllowedFunctionAssemblies).Where(type => MatchesFilter(type.FullName ?? String.Empty)).ToList();
    bool MatchesFilter(string name) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => name.Contains(filter, StringComparison.OrdinalIgnoreCase));
  }

  private ServiceProvider InitialiseDi(List<Type> functypes) {
    var svcs = new ServiceCollection();
    svcs.AddSingleton(settings);
    
    AddService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
    functypes.ForEach(type => svcs.AddSingleton(type));
    ReflectionUtils.GetAllTypesThatImplement(typeof(IFunctionInitialiser), settings.AllowedFunctionAssemblies).ForEach(type => {
      var initialiser = Activator.CreateInstance(type) as IFunctionInitialiser ?? throw new Exception();
      initialiser.RegisterServices(svcs);
    });
    return svcs.BuildServiceProvider();
    
    void AddService<F, I>(string provider) where F : IServiceFactory<I> where I : class {
      svcs.AddSingleton(typeof(F), GetFactoryType<F>(provider));
      svcs.AddSingleton<I>(prov => prov.GetRequiredService<F>().GetService());
    }
    
    Type GetFactoryType<F>(string provider) {
      var potentials = ReflectionUtils.GetAllTypesThatImplement(typeof(F), settings.AllowedFunctionAssemblies); 
      return potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}]");
    }
    
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }
  
  private List<IRunnableFunction> InitialiseFunctions(ServiceProvider svcs, List<Type> types) => 
      types.Select(svcs.GetRequiredService).Cast<IRunnableFunction>().ToList();
}