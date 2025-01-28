using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.AppSheet;

public class AppSheetIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    var core = new SampleCoreStorageRepository(() => new SampleDbContext(), new SqliteDbFieldsHelper());
    svcs.AddSingleton<ICoreStorage>(core);
    svcs.AddSingleton(core);
    svcs.AddSingleton<AppSheetApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await prov.GetRequiredService<SampleCoreStorageRepository>().Initialise();
  }

}