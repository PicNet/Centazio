using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.Tests;

public class SqliteConn {
  
  private const string TEST_FILENAME = "centazio_tests.db"; 
  public static readonly SqliteConn Instance = new();

  public SqliteConn() {
    if (File.Exists(TEST_FILENAME)) File.Delete(TEST_FILENAME);  
  }

  public SqliteConnection Conn() => new($"Data Source={TEST_FILENAME};");
}