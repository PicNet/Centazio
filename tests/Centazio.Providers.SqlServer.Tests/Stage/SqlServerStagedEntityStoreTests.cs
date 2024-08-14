using System.Globalization;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using centazio.core.tests;
using centazio.core.tests.Stage;
using Centazio.Providers.SQLServer.Stage;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.Aws.Tests.Stage;

public class SqlServerStagedEntityStoreTests : StagedEntityStoreDefaultTests {

  protected override async Task<IStagedEntityStore> GetStore(int limit=0) {
    var settings = new SettingsLoader<TestSettings>().Load();
    var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
    return await new TestingSqlServerStagedEntityStore(secrets.SQL_CONN_STR, limit).Initalise();
  }
  
  class TestingSqlServerStagedEntityStore(string connstr, int limit = 100) : SqlServerStagedEntityStore(new SqlServerStagedEntityStoreConfiguration(connstr, TABLE_NAME, limit)) {
    
    private static readonly string TABLE_NAME = nameof(TestingSqlServerStagedEntityStore);
    
    public string ConnStr => connstr;

    public override async ValueTask DisposeAsync() {
      await using var conn = new SqlConnection(connstr);
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {TABLE_NAME}");
      await base.DisposeAsync(); 
    }
  }
  
  [Test] public void Test_sql_datetime_handling() {
    // DATETIME rounds milliseconds so using DATETIME2, see: https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql?view=sql-server-ver15#rounding-of-datetime-fractional-second-precision
    using var conn = new SqlConnection(((TestingSqlServerStagedEntityStore)store).ConnStr);
    var dt1 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME)");
    var dt2 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME2)");
    
    Assert.That(dt1, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.003Z").ToUniversalTime()));
    Assert.That(dt2, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.002Z").ToUniversalTime()));
  }
}

