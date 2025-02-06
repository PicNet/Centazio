using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Type = System.Type;

namespace Centazio.Host;

public class HostBootstrapper(CentazioSettings settings) {

  public async Task<List<IRunnableFunction>> InitHost(IHostConfiguration cmdsetts) {
    FunctionConfigDefaults.ThrowExceptions = true;
    Log.Logger = LogInitialiser.GetFileAndConsoleConfig(cmdsetts.GetLogLevel(), cmdsetts.GetLogFilters()).CreateLogger();
    var assembly = ReflectionUtils.LoadAssembly(cmdsetts.AssemblyName);
    var registrar = new CentazioServicesRegistrar(new ServiceCollection());
    RegisterCoreServices(registrar);
    var integration = IntegrationsAssemblyInspector.GetCentazioIntegration(assembly);
    integration.RegisterServices(registrar);
    var funcs = RegisterCentazioFunctions(registrar, integration, cmdsetts.ParseFunctionFilters());
    var prov = registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await integration.Initialise(prov);
    return InitialiseFunctions(funcs, prov);
  }

  private void RegisterCoreServices(CentazioServicesRegistrar svcs) {
    svcs.Register(settings);
    
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
    void AddCoreService<F, I>(string provider) where F : IServiceFactory<I> where I : class {
      svcs.RegisterServiceTypeFactory(typeof(F), IntegrationsAssemblyInspector.GetCoreServiceFactoryType<F>(provider));
      svcs.Register<I>(prov => prov.GetRequiredService<F>().GetService());
    }
  }

  private List<Type> RegisterCentazioFunctions(CentazioServicesRegistrar registrar, IIntegrationBase integration, List<string> filters) {
    var funcs = IntegrationsAssemblyInspector.GetCentazioFunctions(integration.GetType().Assembly, filters).ForEachAndReturn(t => registrar.Register(t));
    Log.Information($"HostBootstrapper found {funcs.Count} functions in integration[{integration.GetType().Name}]:\n\t" + String.Join("\n\t", funcs.Select(func => func.Name)));
    return funcs;
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }
  
  
  private List<IRunnableFunction> InitialiseFunctions(List<Type> funcs, ServiceProvider prov) {
     return funcs.Select(functype => (IRunnableFunction) prov.GetRequiredService(functype)).ToList();
  }
}