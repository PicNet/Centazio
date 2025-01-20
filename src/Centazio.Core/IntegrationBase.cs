using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core;

public interface IIntegrationBase {
  void RegisterServices(IServiceCollection svcs);
  List<Type> GetAllFunctionTypes();
  Task Initialise(ServiceProvider prov);
}

public abstract class IntegrationBase<TSettings, TSecrets> : IIntegrationBase 
    where TSettings : CentazioSettings
    where TSecrets : CentazioSecrets {

  protected TSettings Settings { get; }
  protected TSecrets Secrets { get; }
  
  protected IntegrationBase(string env = "dev") {
    Settings = new SettingsLoader().Load<TSettings>(env);
    Secrets = new NetworkLocationEnvFileSecretsLoader(Settings.GetSecretsFolder()).Load<TSecrets>(env);
  }
  
  public void RegisterServices(IServiceCollection svcs) {
    svcs.AddSingleton(Settings);
    svcs.AddSingleton(Secrets);
    
    RegisterIntegrationSpecificServices(svcs);
  }

  public abstract List<Type> GetAllFunctionTypes();
  public abstract Task Initialise(ServiceProvider prov);
  
  protected abstract void RegisterIntegrationSpecificServices(IServiceCollection svcs);
}