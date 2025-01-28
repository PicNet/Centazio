using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.AppSheet;

public class AppSheetIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    // todo: this is a duplicate registeration of core storage
    var core = new SampleCoreStorageRepository(() => new SampleDbContext(Settings.CoreStorage.ConnectionString), new SqliteDbFieldsHelper());
    svcs.AddSingleton<ICoreStorage>(core);
    svcs.AddSingleton(core);
    svcs.AddSingleton<AppSheetApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await prov.GetRequiredService<SampleCoreStorageRepository>().Initialise();
  }

}