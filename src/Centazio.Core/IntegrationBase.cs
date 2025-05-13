using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core;

public interface IIntegrationBase {
  Task RegisterServices(CentazioServicesRegistrar registrar);
  Task Initialise(ServiceProvider prov);
}


public abstract class IntegrationBase<TSettings, TSecrets>(params List<string> environments) : IIntegrationBase
    where TSettings : CentazioSettings
    where TSecrets : CentazioSecrets {

  protected TSettings Settings { get; private set; } = null!;
  protected TSecrets Secrets { get; private set; } = null!;

  public async Task RegisterServices(CentazioServicesRegistrar registrar) {
    Settings = await new SettingsLoader().Load<TSettings>(environments);
    Secrets = await new SecretsFileLoader(Settings.GetSecretsFolder()).Load<TSecrets>(environments);
    
    SettingsLoader.RegisterSettingsHierarchy(Settings, registrar);
    registrar.Register(Secrets);
    
    RegisterIntegrationSpecificServices(registrar);
  }

  public abstract Task Initialise(ServiceProvider prov);
  
  protected abstract void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar);
}