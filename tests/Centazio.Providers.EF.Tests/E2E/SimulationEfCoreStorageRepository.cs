﻿using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF.Tests.E2E;

public class SimulationEfCoreStorageRepository(Func<AbstractSimulationCoreStorageDbContext> getdb, IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum, IDbFieldsHelper dbf) : AbstractCoreStorageRepository(checksum) {
  
  public async Task<SimulationEfCoreStorageRepository> Initialise() {
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

  private async Task DropTablesImpl(AbstractSimulationCoreStorageDbContext db) {
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreInvoiceName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreCustomerName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(db.SchemaName, db.CoreMembershipTypeName));
  }

  public override async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<(CoreEntityAndMeta UpdatedCoreEntityAndMeta, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreEntity.CoreId);
    await using var db = getdb();
    entities.ForEach(t => {
      var isexisting = existing.ContainsKey(t.UpdatedCoreEntityAndMeta.CoreEntity.CoreId);
      if (isexisting) { tracker.Update(t.UpdatedCoreEntityAndMeta); } else { tracker.Add(t.UpdatedCoreEntityAndMeta); }
      var dtos = t.UpdatedCoreEntityAndMeta.ToDtos();
      db.Attach(dtos.coreentdto).State = isexisting ? EntityState.Modified : EntityState.Added;
      db.Attach(dtos.metadto).State = isexisting ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntityAndMeta.CoreEntity.DisplayName}({e.UpdatedCoreEntityAndMeta.CoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities.Select(c => c.UpdatedCoreEntityAndMeta).ToList();
  }
  
  protected override async Task<List<CoreEntityAndMeta>> GetList<E, D>(Expression<Func<D, bool>> predicate) {
    await using var db = getdb();
    var cores = await db.Set<D>().Where(predicate).ToListAsync();
    var metas = await db.Metas.Where(m => cores.Select(c => c.CoreId).Contains(m.CoreId)).ToListAsync();
    var lst = cores.Select(dto => {
      var meta = metas.Single(m => m.CoreId == dto.CoreId);
      return new CoreEntityAndMeta(dto.ToBase(), meta.ToBase());
    }).ToList();
    return lst;
  }
  
  protected override async Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : class {
    await using var db = getdb();
    return (await db.Set<D>().SingleAsync(dto => dto.CoreId == coreid.Value)).ToBase();
  }
}