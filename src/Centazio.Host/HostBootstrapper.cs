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
    var types = GetCentazioIntegrations(filters);
    Log.Information($"HostBootstrapper found integrations:\n\t" + String.Join("\n\t", types.Select(type => type.Name)));
    var (prov, funcs) = InitialiseDi(types, filters);
    await InitialiseCoreServices(prov);
    return funcs.Select(functype => (IRunnableFunction) prov.GetRequiredService(functype)).ToList();
  }

  private List<Type> GetCentazioIntegrations(List<string> filters) {
    return ReflectionUtils.GetAllTypesThatImplement(typeof(IntegrationBase<,>), settings.AllowedFunctionAssemblies).Where(type => DoesTypeMatchFilter(type, filters)).ToList();
  }

  private bool DoesTypeMatchFilter(Type type, List<string> filters) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => (type.FullName ?? String.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));

  private (ServiceProvider, List<Type> funcs) InitialiseDi(List<Type> integrations, List<string> filters) {
    var svcs = new ServiceCollection();
    svcs.AddSingleton(settings);
    
    Log.Debug($"HostBootstrapper registering StagedEntityRepository[{settings.StagedEntityRepository.Provider}] and ICtlRepository[{settings.CtlRepository.Provider}]");
    AddService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
    var funcs = integrations.SelectMany(type => {
      var integration = (IIntegrationBase) (Activator.CreateInstance(type) ?? throw new Exception());
      svcs.AddSingleton(type, integration);
      var functypes = integration.GetAllFunctionTypes()
          .Where(functype => DoesTypeMatchFilter(functype, filters))
          .ToList();
      functypes.ForEach(functype => svcs.AddSingleton(functype));
      integration.RegisterServices(svcs);
      return functypes;
    }).ToList();
    return (svcs.BuildServiceProvider(), funcs);
    
    void AddService<F, I>(string provider) where F : IServiceFactory<I> where I : class {
      svcs.AddSingleton(typeof(F), GetFactoryType<F>(provider));
      svcs.AddSingleton<I>(prov => prov.GetRequiredService<F>().GetService());
    }
    
    Type GetFactoryType<F>(string provider) {
      var potentials = ReflectionUtils.GetAllTypesThatImplement(typeof(F), settings.AllowedFunctionAssemblies); 
      return potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}] from assemblies [{String.Join(",", settings.AllowedFunctionAssemblies)}]");
    }
    
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }
}