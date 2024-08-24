using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using centazio.core.tests.Stage;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {

  [Test] public async Task Test_serialisation_of_enums() {
    await repo.CreateObjectState(await repo.CreateSystemState(NAME, NAME), NAME);
    
    await using var conn = SqlConn.Instance.Conn();
    var res = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastResult FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    Assert.That(res, Is.EqualTo(nameof(EOperationReadResult.Unknown)));
  }
  
  protected override async Task<ICtlRepository> GetRepository() 
      => await new TestingSqlServerCtlRepository().Initalise();

  class TestingSqlServerCtlRepository() 
      : SqlServerCtlRepository(() => SqlConn.Instance.Conn(), new TestingUtcDate()) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SCHEMA}.{SYSTEM_STATE_TBL};");
      }
      await base.DisposeAsync(); 
    }
  }
}