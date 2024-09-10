using Centazio.Core.CoreRepo;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.CoreRepo;

public class SqlServerCoreStorageUpserter(Func<SqlConnection> newconn, IDictionary<Type, string> upsertsqls) : ICoreStorageUpserter {
  
  public async Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    await using var conn = newconn();
    await conn.ExecuteAsync(upsertsqls[typeof(T)], entities);
    return entities;
  }

  public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

}