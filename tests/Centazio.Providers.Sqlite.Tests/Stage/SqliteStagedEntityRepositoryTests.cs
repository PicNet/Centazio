using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Stage;

public class SqliteStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var settings = (await F.Settings()).StagedEntityRepository with { ConnectionString = SqliteTestConstants.DEFAULT_CONNSTR };
    return await new EfStagedEntityRepository(new(limit, checksum, () => new SqliteStagedEntityContext(settings)), true).Initialise();
  }

}

