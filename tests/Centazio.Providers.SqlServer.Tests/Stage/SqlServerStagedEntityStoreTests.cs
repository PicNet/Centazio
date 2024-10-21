using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Stage;

public class SqlServerStagedEntityRepositoryTests : StagedEntityRepositoryDefaultTests {

  protected override async Task<IStagedEntityRepository> GetRepository(int limit=0, Func<string, StagedEntityChecksum>? checksum = null) 
      => await new TestingSqlServerStagedEntityRepository(limit, checksum).Initalise();

  class TestingSqlServerStagedEntityRepository(int limit, Func<string, StagedEntityChecksum>? checksum = null) 
      : SqlServerStagedEntityRepository(async () => await SqlConn.Instance.Conn(), limit, checksum ?? Helpers.TestingStagedEntityChecksum ) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = await SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{STAGED_ENTITY_TBL}");
      }
      await base.DisposeAsync(); 
    }
  }

}

