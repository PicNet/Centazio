using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Providers.Sqlite;
using Centazio.Sample.AppSheet;
using Centazio.Sample.ClickUp;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample;

public class SampleIntegration : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(CentazioHostServiceRegistrar registrar) {
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