using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Stage;

public class SqliteStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit=0, Func<string, StagedEntityChecksum>? checksum = null) 
      => await new TestingSqlServerStagedEntityStore(limit, checksum).Initalise();

  class TestingSqlServerStagedEntityStore(int limit, Func<string, StagedEntityChecksum>? checksum = null) 
      : SqliteStagedEntityStore(SqliteConn.Instance.Conn, limit, checksum ?? Helpers.TestingStagedEntityChecksum );

}

