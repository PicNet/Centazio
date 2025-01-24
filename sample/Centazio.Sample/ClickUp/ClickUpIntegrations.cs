using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    var core = new SampleCoreStorageRepository(() => new SampleDbContext(), new SqliteDbFieldsHelper());
    svcs.AddSingleton<ICoreStorage>(core);
    svcs.AddSingleton(core);
    svcs.AddSingleton<ClickUpApi>();
  }

  public override async Task Initialise(ServiceProvider prov) {
    await ((SampleCoreStorageRepository) prov.GetRequiredService<ICoreStorage>()).Initialise();
  }

}