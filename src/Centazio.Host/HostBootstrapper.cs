using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Type = System.Type;

namespace Centazio.Host;

public class HostBootstrapper(CentazioSettings settings, bool quiet) {

  public async Task<List<IRunnableFunction>> InitHost(List<string> filters) {
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig(quiet ? LogEventLevel.Warning : LogEventLevel.Debug).CreateLogger();
    
    var svcs = new ServiceCollection();
    RegisterCoreServices(svcs);
    var integrations = GetCentazioIntegrations();
    var funcs = RegisterCentazioFunctions(svcs, integrations, filters);
    var prov = svcs.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await InitialiseIntegrations(integrations, prov);
    return InitialiseFunctions(funcs, prov);
  }

  private void RegisterCoreServices(ServiceCollection svcs) {
    FunctionConfigDefaults.ThrowExceptions = true;
    
    svcs.AddSingleton(settings);
    
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
    void AddCoreService<F, I>(string provider) where F : IServiceFactory<I> where I : class {
      svcs.AddSingleton(typeof(F), GetCoreServiceFactoryType<F>(provider));
      svcs.AddSingleton<I>(prov => prov.GetRequiredService<F>().GetService());
    }
    
    Type GetCoreServiceFactoryType<F>(string provider) {
      var potentials = ReflectionUtils.GetAllTypesThatImplement(typeof(F), settings.AllowedFunctionAssemblies); 
      return potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}] from assemblies [{String.Join(",", settings.AllowedFunctionAssemblies)}]");
    }
  }

  private List<IIntegrationBase> GetCentazioIntegrations() {
    var integrations = ReflectionUtils.GetAllTypesThatImplement(typeof(IntegrationBase<,>), settings.AllowedFunctionAssemblies).ToList();
    if (!integrations.Any()) throw new Exception("Could not find any Centazio Integrations in provided assemblies");
    return integrations.Select(it => (IIntegrationBase) (Activator.CreateInstance(it) ?? throw new Exception())).ToList();
  }

  private List<Type> RegisterCentazioFunctions(ServiceCollection svcs, List<IIntegrationBase> integrations, List<string> filters) {
    // todo: this could potentially register duplicate services
    integrations.ForEach(i => i.RegisterServices(svcs));
    
    var funcs = integrations.Select(i => i.GetType().Assembly).Distinct().SelectMany(ass => {
      var functypes = ReflectionUtils.GetAllTypesThatImplement(typeof(AbstractFunction<>), ass)
          .Where(DoesTypeMatchFilter)
          .ToList();
      functypes.ForEach(functype => svcs.AddSingleton(functype));
      if (!functypes.Any()) throw new Exception($"Could not find any Centazio Functions in assembly[{ass.GetName().Name}]");
      return functypes;
    }).ToList();
    Log.Information($"HostBootstrapper found {integrations.Count} integrations (and {funcs.Count} functions):\n\t" + String.Join("\n\t", integrations.Select(integration => integration.GetType().Name)));
    return funcs;
    
    bool DoesTypeMatchFilter(Type type) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => (type.FullName ?? String.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }
  
  private async Task InitialiseIntegrations(List<IIntegrationBase> integrations, ServiceProvider prov) => 
      await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
  
  private List<IRunnableFunction> InitialiseFunctions(List<Type> funcs, ServiceProvider prov) {
     return funcs.Select(functype => (IRunnableFunction) prov.GetRequiredService(functype)).ToList();
  }
}