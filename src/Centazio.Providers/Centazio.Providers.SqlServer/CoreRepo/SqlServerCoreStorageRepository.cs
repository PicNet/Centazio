using System.Linq.Expressions;
using Centazio.Core.CoreRepo;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Centazio.Providers.SqlServer.CoreRepo;

public class SqlServerCoreStorageRepository(Func<SqlConnection> newconn, IDictionary<Type, string> upsertsqls) : ICoreStorageRepository {

  
  // Should this be abstract?  By default we should implement `Raw` entities and
  // explicitly cast them to ICoreEntities to ensure proper entity validation is handled.
  // So this default implementation will never (rarely?) be used.
  public virtual async Task<T> Get<T>(string id) where T : ICoreEntity {
    await using var conn = newconn();
    return await conn.QuerySingleAsync<T>($"SELECT * FROM {typeof(T).Name} WHERE Id=@Id", new { Id = id });
  }

  public async Task<T> Upsert<T>(T e) where T : ICoreEntity {
    return (await Upsert(new [] { e })).Single();
  }

  public async Task<IEnumerable<T>> Upsert<T>(IEnumerable<T> entities) where T : ICoreEntity {
    await using var conn = newconn();
    await conn.ExecuteAsync(upsertsqls[typeof(T)], entities);
    return entities;
  }

  public Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity => 
      throw new NotSupportedException("SqlServerCoreStorageRepository using Dapper does not support `Query<T>(Expression<Func<T, bool>> predicate)`.  Use `Query<T>(string query)` instead.");

  // Should this be abstract?  By default we should implement `Raw` entities and
  // explicitly cast them to ICoreEntities to ensure proper entity validation is handled.
  // So this default implementation will never (rarely?) be used. 
  public virtual async Task<IEnumerable<T>> Query<T>(string query) where T : ICoreEntity {
    await using var conn = newconn();
    return await conn.QueryAsync<T>(query);
  }
  
  public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

}