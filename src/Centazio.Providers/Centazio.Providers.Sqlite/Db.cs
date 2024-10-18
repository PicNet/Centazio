using Centazio.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite;

// simple wrappers to assist in adding debug information when executing queries/commands
public static class Db {
  
  public static async Task<int> Exec(SqliteConnection conn, string sql, object? arg = null) {
    try { return await conn.ExecuteAsync(sql, arg); }
    catch (Exception e) {
      var argstr = arg is null ? "n/a" : Json.Serialize(arg);
      throw new Exception($"error running command[{sql}] with arg[{argstr}]", e);
    }
  }
  
  public static async Task<List<T>> Query<T>(SqliteConnection conn, string query, object? arg=null) {
    try { return (await conn.QueryAsync<T>(query, arg)).ToList(); }
    catch (Exception e) {
      var argstr = arg is null ? "n/a" : Json.Serialize(arg);
      throw new Exception($"error running query[{query}] with arg[{argstr}]", e);
    }
  }
}