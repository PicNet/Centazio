using Centazio.Core.Stage;
using Centazio.Core.Tests.Stage;
using Centazio.Providers.SQLServer.Stage;
using Centazio.Providers.SqlServer.Tests;
using Dapper;

namespace Centazio.Providers.Aws.Tests.Stage;

public class SqlServerStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit=0) 
      => await new TestingSqlServerStagedEntityStore(limit).Initalise();

  class TestingSqlServerStagedEntityStore(int limit) 
      : SqlServerStagedEntityStore(() => SqlConn.Instance.Conn(), limit) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{STAGED_ENTITY_TBL}");
      }
      await base.DisposeAsync(); 
    }
  }

}

