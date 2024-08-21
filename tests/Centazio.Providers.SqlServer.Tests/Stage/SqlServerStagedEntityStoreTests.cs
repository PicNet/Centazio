using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.SQLServer.Stage;
using Dapper;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Centazio.Providers.Aws.Tests.Stage;

public class SqlServerStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  
  // during development setting this to false is quicker as dev server is very fast
  private readonly bool USE_CONTAINER = true;
  
  private MsSqlContainer container;
  
  [OneTimeSetUp] public async Task Init() {
    if (!USE_CONTAINER) return;
    container = new MsSqlBuilder().Build();
    await container.StartAsync();
  }

  [OneTimeTearDown] public async Task Cleanup() {
    if (!USE_CONTAINER) return;
    
    await container.StopAsync();
    await container.DisposeAsync();
  }

  private string ConnStr { get {
    if (USE_CONTAINER) return container.GetConnectionString();
    
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return secrets.SQL_CONN_STR;
  } }

  [Test] public void Test_sql_datetime_handling() {
    // DATETIME rounds milliseconds so using DATETIME2, see: https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql?view=sql-server-ver15#rounding-of-datetime-fractional-second-precision
    using var conn = new SqlConnection(ConnStr);
    var dt1 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME)");
    var dt2 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME2)");
    
    Assert.That(dt1, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.003Z").ToUniversalTime()));
    Assert.That(dt2, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.002Z").ToUniversalTime()));
  }

  protected override async Task<IStagedEntityStore> GetStore(int limit=0) {
    return await new TestingSqlServerStagedEntityStore(ConnStr, limit).Initalise();
  }

  class TestingSqlServerStagedEntityStore(string connstr, int limit) : SqlServerStagedEntityStore(connstr, TABLE_NAME, limit) {

    private const string TABLE_NAME = nameof(TestingSqlServerStagedEntityStore);

    public override async ValueTask DisposeAsync() {
      await using var conn = new SqlConnection(ConnStr);
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {TABLE_NAME}");
      await base.DisposeAsync(); 
    }
  }

}

