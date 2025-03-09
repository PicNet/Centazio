using Centazio.Providers.Sqlite;
using Centazio.Sample.AppSheet;
using Centazio.Sample.ClickUp;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample;

public class SampleIntegration(string environment) : IntegrationBase<SampleSettings, SampleSecrets>(environment) {
  
  protected override void RegisterIntegrationSpecificServices(CentazioServicesRegistrar registrar) {
    var core = new SampleCoreStorageRepository(() => new SampleDbContext(Settings.CoreStorage.ConnectionString), new SqliteDbFieldsHelper());
    registrar.Register<ICoreStorage>(core);
    registrar.Register(core);
    registrar.Register<AppSheetApi>();
    registrar.Register<ClickUpApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await prov.GetRequiredService<SampleCoreStorageRepository>().Initialise();
  }

}