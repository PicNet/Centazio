using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.GoogleSheets;

public class GoogleSheetsIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    svcs.AddSingleton<GoogleSheetsApi>();
    svcs.AddSingleton<ICoreStorage>(new SampleCoreStorageRepository(
        () => new SampleDbContext(),
        new SqliteDbFieldsHelper()));
  }

  public override async Task Initialise(ServiceProvider prov) {
    var core = (SampleCoreStorageRepository) prov.GetRequiredService<ICoreStorage>();
    await core.Initialise();
  }

}