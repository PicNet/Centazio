using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings, IChecksumAlgorithm checksum) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EfStagedEntityRepositoryOptions(settings.Limit, checksum.Checksum, () => new SqlServerStagedEntityContext(settings));
    return new EfStagedEntityRepository(opts, settings.CreateSchema);
  }
}
