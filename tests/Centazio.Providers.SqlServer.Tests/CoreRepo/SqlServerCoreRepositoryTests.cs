using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Centazio.Providers.SqlServer.CoreRepo;
using Dapper;

namespace Centazio.Providers.SqlServer.Tests.Ctl;

public class SqlServerCoreRepositoryTests() : CoreStorageRepositoryDefaultTests(false) {

  protected override async Task<ICoreStorageRepository> GetRepository() 
      => await new TestingSqlServerCoreStorageRepository().Initalise();

  public class TestingSqlServerCoreStorageRepository() : SqlServerCoreStorageRepository(
      SqlConn.Instance.Conn, 
      new Dictionary<Type, string> {
        { typeof(CoreCustomer), $@"MERGE INTO {nameof(CoreCustomer)} T
USING (VALUES (@Id, @FirstName, @LastName, @DateOfBirth, @DateCreated, @DateUpdated, @SourceSystemDateUpdated))
  AS c (Id, FirstName, LastName, DateOfBirth, DateCreated, DateUpdated, SourceSystemDateUpdated)
ON T.Id = c.Id
WHEN NOT MATCHED THEN
 INSERT (Id, FirstName, LastName, DateOfBirth, DateCreated, SourceSystemDateUpdated)
 VALUES (c.Id, c.FirstName, c.LastName, c.DateOfBirth, c.DateCreated, c.SourceSystemDateUpdated)
WHEN MATCHED THEN 
  UPDATE SET FirstName=c.FirstName, LastName=c.LastName, DateOfBirth=c.DateOfBirth,
    DateUpdated=c.DateUpdated, SourceSystemDateUpdated=c.SourceSystemDateUpdated;" }
      }) {

    public async Task<ICoreStorageRepository> Initalise() {
      await using var conn = SqlConn.Instance.Conn();
    await conn.ExecuteAsync($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{nameof(CoreCustomer)}' AND xtype='U')
BEGIN
  CREATE TABLE {nameof(CoreCustomer)} (
    Id nvarchar(64) NOT NULL PRIMARY KEY, 
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
    
    public override async Task<T> Get<T>(string id) {
      // todo: this is a bad implementation
      await using var conn = SqlConn.Instance.Conn();
      var raws = (await conn.QueryAsync<CoreCustomerRaw>($"SELECT * FROM {typeof(T).Name} WHERE Id=@Id", new { Id = id })).ToList();
      if (!raws.Any()) throw new Exception($"Core entity [{typeof(T).Name}#{id}] not found");
      return raws.Select(r => (CoreCustomer) r).Cast<T>().Single();
    }
    
    public override async Task<IEnumerable<T>> Query<T>(string query) {
      await using var conn = SqlConn.Instance.Conn();
      var raws = await conn.QueryAsync<CoreCustomerRaw>(query);
      return raws.Select(raw => (CoreCustomer) raw).Cast<T>();
    }

    public override async ValueTask DisposeAsync() {
      if (!SqlConn.Instance.Real) {
        await using var conn = SqlConn.Instance.Conn();
        await conn.ExecuteAsync($"DROP TABLE IF EXISTS {nameof(CoreCustomer)};");
      }
      await base.DisposeAsync(); 
    }
  }
}

public record CoreCustomerRaw {
  public string? Id { get; init; }
  public string? FirstName { get; init; }
  public string? LastName { get; init; }
  public DateOnly? DateOfBirth { get; init; }
  public DateTime? DateUpdated { get; init; } 
  public string? SourceSystem { get; init; } 
  public DateTime? DateCreated { get; init; } 
  public DateTime? SourceSystemDateUpdated { get; init; }
  
  public static explicit operator CoreCustomer(CoreCustomerRaw raw) => new(
      raw.Id ?? throw new ArgumentNullException(nameof(Id)),
      raw.FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
      raw.LastName ?? throw new ArgumentNullException(nameof(LastName)),
      raw.DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)),
      raw.DateUpdated ?? UtcDate.UtcNow);
}