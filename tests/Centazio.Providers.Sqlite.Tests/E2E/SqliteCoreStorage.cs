using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class SqliteCoreStorage(SimulationCtx ctx, Func<SqliteConnection> newconn) : AbstractCoreStorage(ctx.ChecksumAlg.Checksum) {
  
  private static readonly string SCHEMA_PREFIX = nameof(ICoreStorage).Substring(1).ToLower();
  
  private static string Table<E>() where E : ICoreEntity => Table(typeof(E));
  private static string Table(Type e) => Table(CoreEntityTypeName.From(e));
  private static string Table(CoreEntityTypeName typenm) => $"{SCHEMA_PREFIX}_" + typenm.Value.ToLower();
  
  public async Task<SqliteCoreStorage> Initialise() {
    await using var conn = newconn();
    var dbf = new DbFieldsHelper();
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(Table<CoreMembershipType>(), GetFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(Table<CoreCustomer>(), GetFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreCustomer.MembershipCoreId)}]) REFERENCES [{Table<CoreMembershipType>()}]([{nameof(ICoreEntity.CoreId)}])");
    await Db.Exec(conn, dbf.GetSqliteCreateTableScript(Table<CoreInvoice>(), GetFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreInvoice.CustomerCoreId)}]) REFERENCES [{Table<CoreCustomer>()}]([{nameof(ICoreEntity.CoreId)}])");
    return this;
    
    List<DbFieldType> GetFields<E>() {
      var fields = dbf.GetDbFields<E>();
      fields.Add(new DbFieldType(nameof(CoreEntityChecksum), typeof(string), "128", true));
      return fields;
    }
  }
  
  public override async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.UpdatedCoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreId);
    entities.ForEach(e => {
      if (existing.ContainsKey(e.UpdatedCoreEntity.CoreId)) { ctx.Epoch.Update(e.UpdatedCoreEntity); } 
      else { ctx.Epoch.Add(e.UpdatedCoreEntity); }
    });
    
    var dicts = entities.Select(e => {
      var dict = DtoHelpers.ToDtoAsDict(e.UpdatedCoreEntity);
      dict[nameof(CoreEntityChecksum)] = e.UpdatedCoreEntityChecksum;
      return dict;
    }).ToList();
    var flds = dicts.SelectMany(d => d.Keys).Distinct().ToList();
    var updateflds = flds.Where(f => !new [] {nameof(ICoreEntity.CoreId), nameof(ICoreEntity.System), nameof(ICoreEntity.SystemId)}.Contains(f)).ToList();
    
    var sql = $@"
INSERT INTO {Table(coretype)} ({String.Join(',', flds)})
  VALUES ({String.Join(',', flds.Select(f => $"@{f}"))})
ON CONFLICT DO 
  UPDATE SET {String.Join(',', updateflds.Select(f => $"{f}=@{f}"))}
  WHERE CoreId=@CoreId;";
    await using var conn = newconn();
    await Db.Exec(conn, sql, dicts);

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }
  
  protected override async Task<List<E>> GetList<E, D>() {
    await using var conn = newconn();
    var dtos = await conn.QueryAsync<D>($"SELECT * FROM [{Table<E>()}]");
    return dtos.Select(dto => dto.ToBase()).ToList();
  }
  
  protected override async Task<E> GetSingle<E, D>(CoreEntityId? coreid) where E : class {
    await using var conn = newconn();
    var result = await conn.QuerySingleAsync<D>($"SELECT * FROM [{Table<E>()}] WHERE [{nameof(ICoreEntity.CoreId)}] = @CoreId", new { CoreId = coreid });
    return result.ToBase();
  }

  public override async ValueTask DisposeAsync() {
    await using var conn = newconn();
    await conn.ExecuteAsync(@$"
DROP TABLE IF EXISTS [{Table<CoreInvoice>()}]; 
DROP TABLE IF EXISTS [{Table<CoreCustomer>()}]; 
DROP TABLE IF EXISTS [{Table<CoreMembershipType>()}];
");
  }

}