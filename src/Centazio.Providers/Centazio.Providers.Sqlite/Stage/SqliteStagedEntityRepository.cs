using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EfStagedEntityRepositoryOptions(settings.Limit, new Sha256ChecksumAlgorithm().Checksum, () => new SqliteStagedEntityContext(settings));
    return new EfStagedEntityRepository(opts, settings.CreateSchema);
  }
}
