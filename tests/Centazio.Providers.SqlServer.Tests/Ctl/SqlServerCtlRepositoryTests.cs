using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using centazio.core.tests.Stage;
using Centazio.Providers.SqlServer.Ctl;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {

  [Test, Ignore("Dapper does not support Enum->string mapping")] public async Task Test_serialisation_of_enums() {
    await using var conn = SqlConn.Instance.Conn();
    
    var created = await repo.CreateObjectState(await repo.CreateSystemState(NAME, NAME), NAME);
    var lr1 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastResult FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var lav1 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastAbortVote FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var updated = await repo.SaveObjectState(created with { LastResult = EOperationReadResult.Success, LastAbortVote = EOperationAbortVote.Continue });
    var lr2 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastResult FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var lav2 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastAbortVote FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    
    Assert.That(created.LastResult, Is.EqualTo(EOperationReadResult.Unknown));
    Assert.That(created.LastAbortVote, Is.EqualTo(EOperationAbortVote.Unknown));
    Assert.That(lr1, Is.EqualTo(nameof(EOperationReadResult.Unknown)));
    Assert.That(lav1, Is.EqualTo(nameof(EOperationAbortVote.Unknown)));
    
    Assert.That(updated.LastResult, Is.EqualTo(EOperationReadResult.Success));
    Assert.That(updated.LastAbortVote, Is.EqualTo(EOperationAbortVote.Continue));
    Assert.That(lr2, Is.EqualTo(nameof(EOperationReadResult.Success)));
    Assert.That(lav2, Is.EqualTo(nameof(EOperationAbortVote.Continue)));
  }
  
  protected override async Task<ICtlRepository> GetRepository() 
      => await new TestingSqlServerCtlRepository().Initalise();

  class TestingSqlServerCtlRepository() 
      : SqlServerCtlRepository(() => SqlConn.Instance.Conn()) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SCHEMA}.{SYSTEM_STATE_TBL};");
      }
      await base.DisposeAsync(); 
    }
  }
}