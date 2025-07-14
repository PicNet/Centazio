using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.AppSheet;

public class AppSheetIntegration(params List<string> environments) : IntegrationBase<Settings, Secrets>(environments) {
  
  protected override void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar) {
    var core = new CoreStorageRepository(() => new CoreStorageDbContext(Settings.CoreStorage.ConnectionString));
    registrar.Register<ICoreStorage>(core);
    registrar.Register(core);
    registrar.Register<AppSheetApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await prov.GetRequiredService<CoreStorageRepository>().Initialise();
  }

}