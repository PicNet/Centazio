using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Test.Lib.AbstractProviderTests;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCtlRepositoryTests : CtlRepositoryDefaultTests {

  internal static readonly string TEST_TABLE = "dbo.TEST_TABLE";
  
  [Test, Ignore("Dapper does not support Enum->string mapping")] public async Task Test_serialisation_of_enums() {
    await using var conn = SqlConn.Instance.Conn();
    
    var created = await repo.CreateObjectState(await repo.CreateSystemState(NAME, NAME), new SystemEntityType(NAME));
    var lr1 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastResult FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var lav1 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastAbortVote FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var updated = await repo.SaveObjectState(created.Success(UtcDate.UtcNow, EOperationAbortVote.Continue, String.Empty));
    var lr2 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastResult FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    var lav2 = await conn.ExecuteScalarAsync<string>($"SELECT TOP 1 LastAbortVote FROM {SqlServerCtlRepository.SCHEMA}.{SqlServerCtlRepository.OBJECT_STATE_TBL}");
    
    Assert.That(created.LastResult, Is.EqualTo(EOperationResult.Unknown));
    Assert.That(created.LastAbortVote, Is.EqualTo(EOperationAbortVote.Unknown));
    Assert.That(lr1, Is.EqualTo(nameof(EOperationResult.Unknown)));
    Assert.That(lav1, Is.EqualTo(nameof(EOperationAbortVote.Unknown)));
    
    Assert.That(updated.LastResult, Is.EqualTo(EOperationResult.Success));
    Assert.That(updated.LastAbortVote, Is.EqualTo(EOperationAbortVote.Continue));
    Assert.That(lr2, Is.EqualTo(nameof(EOperationResult.Success)));
    Assert.That(lav2, Is.EqualTo(nameof(EOperationAbortVote.Continue)));
  }
  
  [Test, Ignore("No Dapper does not handle proper validation of records, created new Raw objects to handle this")] public async Task Test_Dapper_is_properly_handling_ValidStrings() {
    DapperInitialiser.Initialise(); // is not working?
    
    await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync($"CREATE TABLE {TEST_TABLE} (Valid nvarchar (8) NULL, Sys nvarchar (8) NULL)");
    await conn.ExecuteAsync($"INSERT INTO {TEST_TABLE} VALUES (null, null)");

    Assert.ThrowsAsync<ArgumentNullException>(() => conn.QueryAsync<TestRecord>($"SELECT * FROM {TEST_TABLE}"));
  }
  
  protected override async Task<ICtlRepository> GetRepository() 
      => await new TestingSqlServerCtlRepository().Initalise();

  class TestingSqlServerCtlRepository() 
      : SqlServerCtlRepository(() => SqlConn.Instance.Conn()) {

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {SCHEMA}.{OBJECT_STATE_TBL}; DROP TABLE IF EXISTS {SCHEMA}.{SYSTEM_STATE_TBL}; DROP TABLE IF EXISTS {TEST_TABLE};");
      }
      await base.DisposeAsync(); 
    }
  }
  
  record TestRecord {

    public ValidString Valid { get; } = null!;
    public SystemName Sys { get; } = null!;
  }
}