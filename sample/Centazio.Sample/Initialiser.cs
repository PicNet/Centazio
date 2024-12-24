using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample;

public class Initialiser : IFunctionInitialiser {

  public void RegisterServices(IServiceCollection svcs) {
    svcs.AddSingleton<ClickUpApi>();
    // todo: settings/secrets should be handled by host, maybe add type arguments to IFunctionInitialiser<SampleSettings, SampleSecrets>
    var settings = new SettingsLoader().Load<SampleSettings>("dev");
    svcs.AddSingleton(settings);
    svcs.AddSingleton(new NetworkLocationEnvFileSecretsLoader(settings.GetSecretsFolder()).Load<SampleSecrets>("dev"));
  }

}