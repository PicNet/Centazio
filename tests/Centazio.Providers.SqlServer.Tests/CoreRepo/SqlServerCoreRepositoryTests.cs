using System.Diagnostics;
using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Centazio.Providers.SqlServer.CoreRepo;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {
  
  protected override async Task<ICoreStorageRepository> GetRepository() => await new TestingSqlServerCoreStorageRepository(GetChecksums, GetUpsertSql).Initalise();
  
  private async Task<Dictionary<string, string>> GetChecksums(Type type, List<string> ids) {
    if (type != typeof(CoreCustomer)) throw new UnreachableException();

    await using var conn = SqlConn.Instance.Conn();
    var mapping = await conn.QueryAsync<(string Id, string Checksum)>($"SELECT Id, Checksum FROM {nameof(CoreCustomer)} WHERE Id IN (@ids)", new { ids });
    return mapping.ToDictionary(t => t.Id, t => t.Checksum);
  }
  
  private string GetUpsertSql(Type type) {
    if (type != typeof(CoreCustomer)) throw new UnreachableException();
    
    return $@"MERGE INTO {nameof(CoreCustomer)} T
USING (VALUES (@Id, @Checksum, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated, @SourceSystemDateUpdated))
AS c (Id, Checksum, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated, SourceSystemDateUpdated)
ON T.Id = c.Id
WHEN NOT MATCHED THEN
INSERT (Id, Checksum, FirstName, LastName, DateOfBirth, DateCreated, SourceSystemDateUpdated)
VALUES (c.Id, c.Checksum, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.SourceSystemDateUpdated)
WHEN MATCHED THEN 
UPDATE SET Checksum=c.Checksum, FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth,
  DateUpdated=c.DateUpdated, SourceSystemDateUpdated=c.SourceSystemDateUpdated;";
  }
}

internal class TestingSqlServerCoreStorageRepository(Func<Type, List<string>, Task<Dictionary<string, string>>> GetChecksums, Func<Type, string> GetUpsertSql) 
    : SqlServerCoreStorageUpserter (SqlConn.Instance.Conn, GetChecksums, GetUpsertSql), ICoreStorageRepository {

  public async Task<ICoreStorageRepository> Initalise() {
    await using var conn = SqlConn.Instance.Conn();
  await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{nameof(CoreCustomer)}' AND xtype='U')
BEGIN
CREATE TABLE {nameof(CoreCustomer)} (
  Id nvarchar(64) NOT NULL PRIMARY KEY,
  Checksum nvarchar(64) NOT NULL, 
  FirstName nvarchar (64) NOT NULL, 
  LastName nvarchar (64) NOT NULL, 
  DateOfBirth date NOT NULL,

  DateCreated datetime2 NULL,
  DateUpdated datetime2 NULL,
  SourceSystemDateUpdated datetime2 NULL)
END
");
  return this;
  }
  
  public async Task<T> Get<T>(string id) where T : ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raw = await conn.QuerySingleOrDefaultAsync<CoreCustomerRaw>($"SELECT * FROM {typeof(T).Name} WHERE Id=@Id", new { Id = id });
    if (raw == null) throw new Exception($"Core entity [{typeof(T).Name}#{id}] not found");
    // todo: hack to case back to `T`
    return new [] { (CoreCustomer) raw }.Cast<T>().Single();
  }

  public async Task<IEnumerable<T>> Query<T>(string query) where T : ICoreEntity {
    await using var conn = SqlConn.Instance.Conn();
    var raws = await conn.QueryAsync<CoreCustomerRaw>(query);
    return raws.Select(raw => (CoreCustomer) raw).Cast<T>();
  }
  
  public Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : ICoreEntity => throw new NotImplementedException();

  public override async ValueTask DisposeAsync() {
    if (!SqlConn.Instance.Real) {
      await using var conn = SqlConn.Instance.Conn();
      await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreCustomer)};");
    }
    await base.DisposeAsync(); 
  }
}

public record CoreCustomerRaw {
  public string? Id { get; init; }
  public string? Checksum { get; init; }
  public string? FirstName { get; init; }
  public string? LastName { get; init; }
  public DateOnly? DateOfBirth { get; init; }
  public DateTime? DateUpdated { get; init; } 
  public string? SourceSystem { get; init; } 
  public DateTime? DateCreated { get; init; } 
  public DateTime? SourceSystemDateUpdated { get; init; }
  
  public static explicit operator CoreCustomer(CoreCustomerRaw raw) => new(
      raw.Id ?? throw new ArgumentNullException(nameof(Id)),
      raw.Checksum ?? "",
      raw.FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
      raw.LastName ?? throw new ArgumentNullException(nameof(LastName)),
      raw.DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)),
      raw.DateUpdated ?? UtcDate.UtcNow);
}