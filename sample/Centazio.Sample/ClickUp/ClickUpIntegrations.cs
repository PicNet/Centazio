using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    // todo: this is a duplicate registeration of core storage
    var core = new SampleCoreStorageRepository(() => new SampleDbContext(Settings.CoreStorage.ConnectionString), new SqliteDbFieldsHelper());
    svcs.AddSingleton<ICoreStorage>(core);
    svcs.AddSingleton(core);
    svcs.AddSingleton<ClickUpApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await ((SampleCoreStorageRepository) prov.GetRequiredService<ICoreStorage>()).Initialise();
  }

}