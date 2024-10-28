using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF.Tests.E2E;

// todo: rename all EFCore -> EF as EFCore and CoreStorage are confusing
public class EfCoreStorageRepository(Func<AbstractCoreStorageDbContext> getdb, IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum, IDbFieldsHelper dbf) : AbstractCoreStorageRepository(checksum) {
  
  public async Task<EfCoreStorageRepository> Initialise() {
    await using var db = getdb();
    await DropTablesImpl(db);
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreMembershipTypeName, dbf.GetDbFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreCustomerName, dbf.GetDbFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreCustomer.MembershipCoreId)}]) REFERENCES [{db.CoreMembershipTypeName}]([{nameof(ICoreEntity.CoreId)}])");
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(db.SchemaName, db.CoreInvoiceName, dbf.GetDbFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreInvoice.CustomerCoreId)}]) REFERENCES [{db.CoreCustomerName}]([{nameof(ICoreEntity.CoreId)}])");
    return this;
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = getdb();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(AbstractCoreStorageDbContext db) {
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreInvoiceName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreCustomerName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreMembershipTypeName));
  }

  public override async Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    // todo: clean this code
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.UpdatedCoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreId);
    await using var db = getdb();
    entities.ForEach(t => {
      var e = (CoreEntityBase) t.UpdatedCoreEntity;
      var dto = DtoHelpers.ToDto(e) ?? throw new Exception();
      db.Attach(dto);
      if (existing.ContainsKey(e.CoreId)) { tracker.Update(e); } 
      else { tracker.Add(e); }
      db.Entry(dto).State = existing.ContainsKey(e.CoreId) ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities.Select(c => c.UpdatedCoreEntity).ToList();
  }
  
  protected override async Task<List<E>> GetList<E, D>() {
    await using var db = getdb();
    var lst = (await db.Set<D>().ToListAsync()).Select(dto => dto.ToBase()).ToList();
    return lst;
  }
  
  // todo: why is coreid nullable
  protected override async Task<E> GetSingle<E, D>(CoreEntityId? coreid) where E : class {
    if (coreid is null) throw new Exception("todo: why is coreid nullable");
    await using var db = getdb();
    return (await db.Set<D>().SingleAsync(dto => dto.CoreId == coreid.Value)).ToBase();
  }
}