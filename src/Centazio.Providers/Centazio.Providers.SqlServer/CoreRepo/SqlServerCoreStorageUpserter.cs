using Centazio.Core.CoreRepo;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.CoreRepo;

// todo: this class seems wasteful, all it does is wrap strategies for each method, why is it even required
public class SqlServerCoreStorageUpserter(
    Func<SqlConnection> GetConnection,
    Func<Type, List<string>, Task<Dictionary<string, string>>> GetChecksumsImpl,
    Func<Type, string> GetUpsertSql) : ICoreStorageUpserter {

  public async Task<Dictionary<string, string>> GetChecksums<T>(List<T> entities) where T : ICoreEntity {
    var checksums = new Dictionary<string, string>();
    if (!entities.Any()) return checksums;
    var type = entities.First().GetType();
    var ids = entities.Select(e => e.Id).ToList();
    return await GetChecksumsImpl(type, ids);
  }

  public async Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    var sql = GetUpsertSql(typeof(T));
    
    await using var conn = GetConnection();
    await conn.ExecuteAsync(sql, entities);
    return entities;
  }

  public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

}