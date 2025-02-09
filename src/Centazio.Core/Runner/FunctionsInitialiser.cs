using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Core.Runner;

public class FunctionsInitialiser {
  private CentazioSettings Settings { get; }
  private CentazioServicesRegistrar Registrar { get; }

  public FunctionsInitialiser() {
    (Settings, Registrar) = (LoadSettings(), new CentazioServicesRegistrar(new ServiceCollection()));
  }

  public async Task<IRunnableFunction> Init<F>() where F : IRunnableFunction => (await Init([typeof(F)])).Single();
  
  public async Task<List<IRunnableFunction>> Init(List<Type> functions) {
    RegisterCoreServices();
    var assemblies = functions.Select(f => f.Assembly).Distinct().ToList();
    var integrations = assemblies.Select(IntegrationsAssemblyInspector.GetCentazioIntegration).ToList();
    integrations.ForEach(integration => integration.RegisterServices(Registrar));
    functions.ForEach(func => Registrar.Register(func));
    var prov = Registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
    
    return functions.Select(func => (IRunnableFunction) prov.GetRequiredService(func)).ToList();
  }

  private CentazioSettings LoadSettings() {
    return new SettingsLoader().Load<CentazioSettings>("dev");
  }
  

  private void RegisterCoreServices() {
    SettingsLoader.RegisterSettingsHierarchy(Settings, Registrar);
    
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{Settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{Settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(Settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(Settings.CtlRepository.Provider);
    
    void AddCoreService<SF, I>(string provider) where SF : IServiceFactory<I> where I : class {
      Registrar.RegisterServiceTypeFactory(typeof(SF), IntegrationsAssemblyInspector.GetCoreServiceFactoryType<SF>(provider));
      Registrar.Register<I>(prov => prov.GetRequiredService<SF>().GetService());
    }
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }

}