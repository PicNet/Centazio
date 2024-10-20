using System.Data.Common;
using Dapper;
using Serilog;

namespace Centazio.Providers.Sqlite;

public static class Db {
  private const bool DEBUG_SQL = true;
  
  public static async Task<int> Exec(DbConnection conn, string sql, object? arg = null) { 
    sql = sql.Trim();
    try { 
      var result = await conn.ExecuteAsync(sql, arg);
      if (DEBUG_SQL) Log.Debug("Db.Exec[Success]:{@Command}\nArgs:{@Args}\nResult:{@Result}", sql, arg, result);
      return result;
    } catch (Exception e) {
      Log.Error("Db.Exec[Error({@ErrorMessage})]:{@Command}\nArgs:{@Args}", e.Message, sql, arg);
      throw;
    }
  }
  
  public static async Task<List<T>> Query<T>(DbConnection conn, string query, object? arg=null) {
    query = query.Trim();
    try { 
      var results = (await conn.QueryAsync<T>(query, arg)).ToList();
      if (DEBUG_SQL) Log.Debug("Db.Query[Success]:{@Query}\nArgs:{@Args}\nResult:{@Result}", query, arg, results.Count);
      return results;
    } catch (Exception e) {
      Log.Error("Db.Query[Error({@ErrorMessage})]:{@Command}\nArgs:{@Args}", e.Message, query, arg);
      throw;
    }
  }
}