using Centazio.Core.Ctl;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Runner;

public class FunctionsInitialiser(string[] environments, CentazioServicesRegistrar registrar) {

  public async Task<IRunnableFunction> Init<F>() where F : IRunnableFunction => (await Init([typeof(F)])).Single();
  
  public async Task<List<IRunnableFunction>> Init(List<Type> functions) {
    var settings = SettingsLoader.RegisterSettingsHierarchy(new SettingsLoader().Load<CentazioSettings>(environments), registrar);
    var secrets = new SecretsFileLoader(settings.GetSecretsFolder()).Load<CentazioSecrets>(environments.First());
    
    RegisterCoreServices(settings);
    var assemblies = functions.Select(f => f.Assembly).Distinct().ToList();
    var integrations = assemblies.Select(ass => IntegrationsAssemblyInspector.GetCentazioIntegration(ass, settings, secrets)).ToList();
    integrations.ForEach(integration => integration.RegisterServices(registrar));
    functions.ForEach(registrar.Register);
    var prov = registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
    
    return functions.Select(func => (IRunnableFunction) prov.GetRequiredService(func)).ToList();
  }
  

  private void RegisterCoreServices(CentazioSettings settings) {
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CtlRepository.Provider);
    
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