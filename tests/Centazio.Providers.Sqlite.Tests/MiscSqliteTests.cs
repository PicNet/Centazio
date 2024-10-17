using Dapper;

namespace Centazio.Providers.Sqlite.Tests;

public class MiscSqliteTests {

  [Test] public void Test_sql_datetime_handling() {
    using var conn = SqliteConn.Instance.Conn();
    var dt = DateTime.Parse("01/01/2024 23:01:01.003Z").ToUniversalTime();
    var dt2 = conn.ExecuteScalar<DateTime>($"SELECT '{dt:o}'");
    
    Assert.That(dt, Is.EqualTo(dt2));
  }

}