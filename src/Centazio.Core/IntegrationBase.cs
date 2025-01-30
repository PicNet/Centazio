using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Centazio.Core;

public interface IIntegrationBase {
  void RegisterServices(IServiceCollection svcs);
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
    SettingsLoader.RegisterSettingsAndRecordPropertiesAsSingletons(Settings, svcs);
    svcs.TryAdd(ServiceDescriptor.Singleton(Secrets));
    
    RegisterIntegrationSpecificServices(svcs);
  }

  public abstract Task Initialise(ServiceProvider prov);
  
  protected abstract void RegisterIntegrationSpecificServices(IServiceCollection svcs);
}