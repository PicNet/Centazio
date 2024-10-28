using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Stage;

public class SqliteStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) 
      => await new TestingEFCoreStagedEntityRepository(new(limit, checksum, () => new SqliteStagedEntityContext()), new SqliteDbFieldsHelper()).Initialise();
}

