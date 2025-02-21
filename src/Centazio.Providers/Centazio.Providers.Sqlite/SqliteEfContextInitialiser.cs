using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite;

public class SqliteEfContextInitialiser {
  private static SqliteConnection? memoryconnection;
  
  public static void SetSqliteOnDbContextOpts(DbContextOptionsBuilder options, string connstr) {
    if (connstr.IndexOf("memory", StringComparison.OrdinalIgnoreCase) < 0) {
      options.UseSqlite(connstr);
      return;
    }
    if (memoryconnection is null) {
      memoryconnection = new SqliteConnection(connstr);
      memoryconnection.Open();
    }
    options.UseSqlite(memoryconnection);
  }
  

}