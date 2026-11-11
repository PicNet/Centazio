using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings, TimeProvider clock, IGuidGenerator ids, IChecksumAlgorithm checksum) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EfStagedEntityRepositoryOptions(settings.Limit, checksum.Checksum, () => new SqlServerStagedEntityContext(settings), clock, ids);
    return new EfStagedEntityRepository(opts, settings.CreateSchema);
  }
}
