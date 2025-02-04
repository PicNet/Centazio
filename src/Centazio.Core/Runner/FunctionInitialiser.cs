using System.Reflection;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Core.Runner;

// todo: lots of duplicate code here, merge `HostBootstrapper.cs`
public class FunctionInitialiser<F> where F : IRunnableFunction {
  private Type FuncType { get; }
  private Assembly FuncAss { get; }
  private CentazioSettings Settings { get; }
  private CentazioHostServiceRegistrar Registrar { get; }

  public FunctionInitialiser() {
    InitialiseLogger();
    (FuncType, FuncAss, Settings) = (typeof(F), typeof(F).Assembly, LoadSettings());
    Registrar = new CentazioHostServiceRegistrar(new ServiceCollection());
  }

  public async Task<IRunnableFunction> Init() {
    RegisterCoreServices();
    var integration = IntegrationsAssemblyInspector.GetCentazioIntegration(FuncAss);
    integration.RegisterServices(Registrar);
    Registrar.Register(FuncType);
    var prov = Registrar.BuildServiceProvider();
    
    await InitialiseCoreServices(prov);
    await integration.Initialise(prov);
    return (IRunnableFunction) prov.GetRequiredService(FuncType);
  }

  private CentazioSettings LoadSettings() {
    return new SettingsLoader().Load<CentazioSettings>("dev");
  }

  private void InitialiseLogger() => Log.Logger = LogInitialiser.GetConsoleConfig().CreateLogger();

  private void RegisterCoreServices() {
    Registrar.Register(Settings);
    
    Log.Debug($"HostBootstrapper registering core services:" +
        $"\n\tStagedEntityRepository [{Settings.StagedEntityRepository.Provider}]" +
        $"\n\tCtlRepository [{Settings.CtlRepository.Provider}]");
    
    AddCoreService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(Settings.StagedEntityRepository.Provider);
    AddCoreService<IServiceFactory<ICtlRepository>, ICtlRepository>(Settings.CtlRepository.Provider);
    
    void AddCoreService<SF, I>(string provider) where SF : IServiceFactory<I> where I : class {
      Registrar.RegisterServiceTypeFactory(typeof(SF), IntegrationsAssemblyInspector.GetCoreServiceFactoryType<SF>(provider, FuncAss));
      Registrar.Register<I>(prov => prov.GetRequiredService<SF>().GetService());
    }
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }

}