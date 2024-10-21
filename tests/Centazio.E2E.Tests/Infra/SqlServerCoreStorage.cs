using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Providers.Sqlite;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Centazio.E2E.Tests.Infra;

// todo: use SqlConnection - will need new nuget, leave for now
// todo: implement
public class SqlServerCoreStorage(SimulationCtx ctx, Func<SqliteConnection> newconn) : AbstractCoreStorage(ctx.ChecksumAlg.Checksum) {
  
  private static readonly string SCHEMA = nameof(ICoreStorage).Substring(1).ToLower();
  private static readonly string MEM_TYPE_TBL = nameof(CoreMembershipType).ToLower();
  private static readonly string CUST_TYPE_TBL = nameof(CoreCustomer).ToLower();
  private static readonly string INV_TYPE_TBL = nameof(CoreInvoice).ToLower();
  private static string Table<E>() where E : ICoreEntity => 
      typeof(E) == typeof(CoreMembershipType) ? MEM_TYPE_TBL
      : typeof(E) == typeof(CoreCustomer) ? CUST_TYPE_TBL 
      : typeof(E) == typeof(CoreInvoice) ? INV_TYPE_TBL 
      : throw new NotSupportedException(typeof(E).Name);
  
  
  public async Task<SqlServerCoreStorage> Initialise() {
    await using var conn = newconn();
    var dbf = new DbFieldsHelper();
    await Db.Exec(conn, dbf.GetSqlServerCreateTableScript(SCHEMA, MEM_TYPE_TBL, dbf.GetDbFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
    await Db.Exec(conn, dbf.GetSqlServerCreateTableScript(SCHEMA, CUST_TYPE_TBL, dbf.GetDbFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreCustomer.MembershipCoreId)}]) REFERENCES [{SCHEMA}].[{MEM_TYPE_TBL}]([{nameof(ICoreEntity.CoreId)}])");
    await Db.Exec(conn, dbf.GetSqlServerCreateTableScript(SCHEMA, INV_TYPE_TBL, dbf.GetDbFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreInvoice.CustomerCoreId)}]) REFERENCES [{SCHEMA}].[{CUST_TYPE_TBL}]([{nameof(ICoreEntity.CoreId)}])");
    return this;
  }
  
  public override Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    throw new NotImplementedException();
  }
  
  protected override async Task<List<E>> GetList<E, D>() {
    await using var conn = newconn();
    var dtos = await conn.QueryAsync<D>($"SELECT * FROM [{SCHEMA}].[{Table<E>()}]");
    return dtos.Select(dto => dto.ToBase()).ToList();
  }
  
  protected override async Task<E> GetSingle<E, D>(CoreEntityId? coreid) where E : class {
    await using var conn = newconn();
    var result = await conn.QuerySingleAsync<D>($"SELECT TOP 1 * FROM [{SCHEMA}].[{Table<E>()}] WHERE [{nameof(ICoreEntity.CoreId)}] = @CoreId", new { CoreId = coreid });
    return result.ToBase();
  }

  public override async ValueTask DisposeAsync() {
    await using var conn = newconn();
    await conn.ExecuteAsync(@$"
DROP TABLE IF EXISTS [{SCHEMA}].[{MEM_TYPE_TBL}]; 
DROP TABLE IF EXISTS [{SCHEMA}].[{CUST_TYPE_TBL}]; 
DROP TABLE IF EXISTS [{SCHEMA}].[{INV_TYPE_TBL}];
");
  }

}