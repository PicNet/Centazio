using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Providers.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {
  
  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    svcs.AddSingleton<ClickUpApi>();
    svcs.AddSingleton<ICoreStorage>(new SampleCoreStorageRepository(
        () => new SampleDbContext(),
        new SqliteDbFieldsHelper(), 
        new Sha256ChecksumAlgorithm().Checksum));
  }

  public override async Task Initialise(ServiceProvider prov) {
    var core = (SampleCoreStorageRepository) prov.GetRequiredService<ICoreStorage>();
    await core.Initialise();
  }

}