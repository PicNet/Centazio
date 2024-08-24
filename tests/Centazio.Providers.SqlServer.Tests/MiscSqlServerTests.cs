using Dapper;

namespace Centazio.Providers.SqlServer.Tests;

public class MiscSqlServerTests {

  [Test] public void Test_sql_datetime_handling() {
    // DATETIME rounds milliseconds so using DATETIME2, see: https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql?view=sql-server-ver15#rounding-of-datetime-fractional-second-precision
    using var conn = SqlConn.Instance.Conn();
    var dt1 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME)");
    var dt2 = conn.ExecuteScalar<DateTime>("SELECT CAST('01/01/2024 23:01:01.002' AS DATETIME2)");
    
    Assert.That(dt1, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.003Z").ToUniversalTime()));
    Assert.That(dt2, Is.EqualTo(DateTime.Parse("01/01/2024 23:01:01.002Z").ToUniversalTime()));
  }

}