using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core;

public interface IIntegrationBase {
  void RegisterServices(CentazioServicesRegistrar registrar);
  Task Initialise(ServiceProvider prov);
}


public abstract class IntegrationBase<TSettings, TSecrets>(TSettings Settings, TSecrets Secrets) : IIntegrationBase 
    where TSettings : CentazioSettings
    where TSecrets : CentazioSecrets {

  protected TSettings Settings { get; } = Settings;
  protected TSecrets Secrets { get; } = Secrets;
  
  public void RegisterServices(CentazioServicesRegistrar registrar) {
    SettingsLoader.RegisterSettingsHierarchy(Settings, registrar);
    registrar.Register(Secrets);
    
    RegisterIntegrationSpecificServices(registrar);
  }

  public abstract Task Initialise(ServiceProvider prov);
  
  protected abstract void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar);
}