using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Stage;

public class SqliteStagedEntityRepositoryTests : StagedEntityRepositoryDefaultTests {

  protected override async Task<IStagedEntityRepository> GetRepository(int limit=0, Func<string, StagedEntityChecksum>? checksum = null) 
      => await new TestingSqlServerStagedEntityRepository(limit, checksum).Initialise();

  class TestingSqlServerStagedEntityRepository(int limit, Func<string, StagedEntityChecksum>? checksum = null) 
      : EFCoreStagedEntityRepository(limit, checksum ?? Helpers.TestingStagedEntityChecksum);

}

