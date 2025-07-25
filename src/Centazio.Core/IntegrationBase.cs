﻿using Centazio.Core.Secrets;
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

  public virtual async Task RegisterServices(CentazioServicesRegistrar registrar) {
    Settings = await new SettingsLoader().Load<TSettings>(environments);
    SettingsLoader.RegisterSettingsHierarchy(Settings, registrar);
    
    registrar.Register(prov => {
      var loader = prov.GetRequiredService<IServiceFactory<ISecretsLoader>>().GetService();
      // `.Results` is required as a hack as `await` is not supported during DI initialisation
      return Secrets = loader.Load<TSecrets>(environments).Result;
    });
    
    RegisterIntegrationSpecificServices(registrar);
  }

  public abstract Task Initialise(ServiceProvider prov);
  
  protected abstract void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar);
}