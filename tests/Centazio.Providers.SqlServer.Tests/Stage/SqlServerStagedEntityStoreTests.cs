using Centazio.Core.Stage;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Stage;

public class SqlServerStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit=0, Func<string, string>? checksum = null) 
      => await new TestingSqlServerStagedEntityStore(limit, checksum).Initalise();

  class TestingSqlServerStagedEntityStore(int limit, Func<string, string>? checksum = null) 
      : SqlServerStagedEntityStore(() => SqlConn.Instance.Conn(), limit, checksum ?? Helpers.TestingChecksum ) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{STAGED_ENTITY_TBL}");
      }
      await base.DisposeAsync(); 
    }
  }

}

