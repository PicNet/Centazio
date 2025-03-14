using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Runner;

public class FunctionsInitialiser {
  
  private readonly string environment;
  private CentazioSettings Settings { get; }
  private CentazioServicesRegistrar registrar { get; }

  public FunctionsInitialiser(string environment, CentazioServicesRegistrar registrar) {
    (this.environment, this.registrar) = (environment, registrar);
    Settings = LoadSettings();
  }

  public async Task<IRunnableFunction> Init<F>() where F : IRunnableFunction => (await Init([typeof(F)])).Single();
  
  public async Task<List<IRunnableFunction>> Init(List<Type> functions) {
    RegisterCoreServices();
    var assemblies = functions.Select(f => f.Assembly).Distinct().ToList();
    var integrations = assemblies.Select(ass => IntegrationsAssemblyInspector.GetCentazioIntegration(ass, environment)).ToList();
    integrations.ForEach(integration => integration.RegisterServices(registrar));
    functions.ForEach(func => registrar.Register(func));
    var prov = registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
    
    return functions.Select(func => (IRunnableFunction) prov.GetRequiredService(func)).ToList();
  }

  private CentazioSettings LoadSettings() => 
      SettingsLoader.RegisterSettingsHierarchy(new SettingsLoader().Load<CentazioSettings>(environment), registrar);

  private void RegisterCoreServices() {
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{Settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{Settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(Settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(Settings.CtlRepository.Provider);
    
    void AddCoreService<SF, I>(string provider) where SF : IServiceFactory<I> where I : class {
      registrar.RegisterServiceTypeFactory(typeof(SF), IntegrationsAssemblyInspector.GetCoreServiceFactoryType<SF>(provider));
      registrar.Register<I>(prov => prov.GetRequiredService<SF>().GetService());
    }
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }

}