using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Runner;


// this needs to be implemented and used by the hosting environment for each cloud provider 
public interface ILazyFunctionInitialiser {
  Task<IRunnableFunction> GetFunction();
  Task<IFunctionRunner> GetRunner();
}

public abstract class AbstractLazyFunctionInitialiser : ILazyFunctionInitialiser {
  
  private readonly CentazioServicesRegistrar registrar;
  private readonly Lazy<Task<IRunnableFunction>> impl;

  protected AbstractLazyFunctionInitialiser(List<string> environments, Type function) {
    registrar = new CentazioServicesRegistrar(new ServiceCollection());
    impl = new(async () => {
      await RegisterEnvironmentDependencies(registrar);
      
      var initialiser = new FunctionsInitialiser(environments, registrar);
      await initialiser.Init([function]);
      return registrar.Get<IRunnableFunction>(function);
    }, LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public async Task<IRunnableFunction> GetFunction() => await impl.Value;
  public async Task<IFunctionRunner> GetRunner() {
    _ = await impl.Value;
    return registrar.Get<IFunctionRunner>();
  }

  // Register FunctionRunner and other cloud specific dependencies
  protected abstract Task RegisterEnvironmentDependencies(CentazioServicesRegistrar registrar); 
}

public class FunctionsInitialiser(List<string> environments, CentazioServicesRegistrar registrar) {

  public async Task Init(List<Type> functions) {
    var settings = SettingsLoader.RegisterSettingsHierarchy(await new SettingsLoader().Load<CentazioSettings>(environments), registrar);
    
    RegisterCoreServices(settings);
    var assemblies = functions.Select(f => f.Assembly).Distinct().ToList();
    var integrations = assemblies.Select(ass => IntegrationsAssemblyInspector.GetCentazioIntegration(ass, environments)).ToList();
    integrations.ForEach(integration => integration.RegisterServices(registrar));
    functions.ForEach(registrar.Register);
    var prov = registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await Task.WhenAll(integrations.Select(integration => integration.Initialise(prov)));
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