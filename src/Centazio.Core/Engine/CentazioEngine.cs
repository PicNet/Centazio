using Centazio.Core.Ctl;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Engine;

/// <summary>
/// The CentazioEngine is the entry point into the Centazio core processor.  And has logic to initialise the
/// engine and kick of the running of functions.
///
/// Specific Hosts (AWS/Azure/Self) should inherit from this class and implement their own wiring between the
/// host and the Centazio engine.  This 'wiring-up' should be done by implementing the `RegisterHostSpecificServices`
/// method.
/// </summary>
public abstract class CentazioEngine(List<string> environments) {

  private readonly CentazioServicesRegistrar registrar = new (new ServiceCollection());
  private ServiceProvider? prov;
  
  protected abstract void RegisterHostSpecificServices(CentazioServicesRegistrar registrar);
  
  public async Task<ServiceProvider> Init(List<Type> functions) {
    if (prov is not null) throw new Exception("CentazioEngine.Init has already been called");
    
    var integrations = await RegisterServices();
    prov = registrar.BuildServiceProvider();
    await InitialiseServices();

    async Task<List<IIntegrationBase>> RegisterServices() {
      var settings = SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader().Load<CentazioSettings>(environments), registrar);
      RegisterCoreServices(settings);
      RegisterHostSpecificServices(registrar);
      return RegisterAvailableFunctionsAndIntegrations(functions);
    }

    async Task InitialiseServices() {
      await InitialiseCoreServices();
      await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
    }
    
    return prov;
  }
  
  public async Task<FunctionRunResults> RunFunction(Type func, List<FunctionTrigger> triggers) {
    if (prov is null) throw new Exception("CentazioEngine.Init has not been called");
    var (runner, function) = (prov.GetRequiredService<IFunctionRunner>(), (IRunnableFunction) prov.GetRequiredService(func));
    return await runner.RunFunction(function, triggers);
  }

  private void RegisterCoreServices(CentazioSettings settings) {
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<ISecretsLoader>, ISecretsLoader>(settings.SecretsLoaderSettings.Provider);
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
    void AddCoreService<SF, I>(string provider) where SF : IServiceFactory<I> where I : class {
      registrar.RegisterServiceTypeFactory(typeof(SF), IntegrationsAssemblyInspector.GetCoreServiceFactoryType<SF>(provider));
      registrar.Register<I>(svcs => svcs.GetRequiredService<SF>().GetService());
    }
  }

  private List<IIntegrationBase> RegisterAvailableFunctionsAndIntegrations(List<Type> functypes) {
    var assemblies = functypes.Select(f => f.Assembly).Distinct().ToList();
    var integrations = assemblies.Select(ass => IntegrationsAssemblyInspector.GetCentazioIntegration(ass, environments)).ToList();
    integrations.ForEach(integration => integration.RegisterServices(registrar));
    functypes.ForEach(registrar.Register);
    return integrations;
  }

  private async Task InitialiseCoreServices() {
    if (prov is null) throw new Exception("CentazioEngine.Init has not been called");
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }

}