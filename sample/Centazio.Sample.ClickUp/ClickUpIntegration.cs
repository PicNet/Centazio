using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegration(params List<string> environments) : IntegrationBase<Settings, Secrets>(environments) {
  
  protected override void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar) {
    var core = new CoreStorageRepository(() => new CoreStorageDbContext(Settings.CoreStorage.ConnectionString));
    registrar.Register<ICoreStorage>(core);
    registrar.Register(core);
    registrar.Register<ClickUpApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await prov.GetRequiredService<CoreStorageRepository>().Initialise();
  }

}