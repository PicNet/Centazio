using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;

namespace Centazio.Providers.PostgresSql.Stage;

public class PostgresSqlStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() {
    var opts = new EFStagedEntityRepositoryOptions(settings.Limit, new Sha256ChecksumAlgorithm().Checksum, () => new PostgresSqlStagedEntityContext(settings));
    return new PostgresSqlStagedEntityRepository(opts, settings.CreateSchema);
  }
}

// todo GT: is this subclass required?
public class PostgresSqlStagedEntityRepository(EFStagedEntityRepositoryOptions opts, bool createschema) : EFStagedEntityRepository(opts, createschema);