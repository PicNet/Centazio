using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EFStagedEntityRepositoryOptions(settings.Limit, new Sha256ChecksumAlgorithm().Checksum, () => new SqliteStagedEntityContext(settings));
    return new SqliteStagedEntityRepository(opts, settings.CreateSchema);
  }
}

public class SqliteStagedEntityRepository(EFStagedEntityRepositoryOptions opts, bool createschema) : EFStagedEntityRepository(opts, createschema);