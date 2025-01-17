using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Centazio.Providers.EF.Tests.E2E;

public class SimulationEfCoreStorageRepository(Func<CentazioDbContext> getdb, IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum, IDbFieldsHelper dbf) : AbstractCoreStorageRepository(checksum) {
  
  private static string CoreSchemaName => "dbo";
  private static string CtlSchemaName { get; } = nameof(Core.Ctl).ToLower();
  private static string CoreStorageMetaName { get; } = nameof(CoreStorageMeta).ToLower();
  private static string CoreMembershipTypeName { get; } = nameof(CoreMembershipType).ToLower();
  private static string CoreCustomerName { get; } = nameof(CoreCustomer).ToLower();
  private static string CoreInvoiceName { get; } = nameof(CoreInvoice).ToLower();
  
  public static void CreateSimulationCoreStorageEfModel(ModelBuilder builder) => builder
      .HasDefaultSchema(CoreSchemaName)
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName, CtlSchemaName);
        e.HasKey(e2 => new { e2.CoreEntityTypeName, e2.CoreId });
      })
      .Entity<CoreMembershipType.Dto>(e => {
        e.ToTable(CoreMembershipTypeName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreCustomer.Dto>(e => {
        e.ToTable(CoreCustomerName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreInvoice.Dto>(e => {
        e.ToTable(CoreInvoiceName);
        e.HasKey(e2 => e2.CoreId);
      });
  
  public async Task<SimulationEfCoreStorageRepository> Initialise() {
    await using var db = getdb();
    await DropTablesImpl(db);
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CtlSchemaName, CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CoreSchemaName, CoreMembershipTypeName, dbf.GetDbFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CoreSchemaName, CoreCustomerName, dbf.GetDbFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreCustomer.MembershipCoreId)}]) REFERENCES [{CoreMembershipTypeName}]([{nameof(ICoreEntity.CoreId)}])");
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CoreSchemaName, CoreInvoiceName, dbf.GetDbFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreInvoice.CustomerCoreId)}]) REFERENCES [{CoreCustomerName}]([{nameof(ICoreEntity.CoreId)}])");
    return this;
  }

  public override async ValueTask DisposeAsync() {
    await using var db = getdb();
    await DropTablesImpl(db);
  }

  private async Task DropTablesImpl(CentazioDbContext db) {
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CoreSchemaName, CoreInvoiceName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CoreSchemaName, CoreCustomerName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CoreSchemaName, CoreMembershipTypeName));
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CtlSchemaName, CoreStorageMetaName));
  }

  public override async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.CoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreEntity.CoreId);
    await using var db = getdb();
    entities.ForEach(t => {
      var isexisting = existing.ContainsKey(t.CoreEntity.CoreId);
      if (isexisting) { tracker.Update(t); } else { tracker.Add(t); }
      var dtos = t.ToDtos();
      db.Attach(dtos.CoreEntityDto).State = isexisting ? EntityState.Modified : EntityState.Added;
      db.Attach(dtos.MetaDto).State = isexisting ? EntityState.Modified : EntityState.Added;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities;
  }

  protected override async Task<List<CoreEntityAndMeta>> GetExistingEntities<E, D>(List<CoreEntityId> coreids)  {
    await using var db = getdb();
    var strids = coreids.Select(id => id.Value);
    return (await db.Set<D>()
        .Join(db.Set<CoreStorageMeta.Dto>(), d => d.CoreId, m => m.CoreId, (e, m) => new { CoreEntity=e, Meta=m })
        .Where(dtos => strids.Contains(dtos.Meta.CoreId))
        .ToListAsync())
        .Select(dto => new CoreEntityAndMeta(dto.CoreEntity.ToBase(), dto.Meta.ToBase()))
        .ToList();
  }

  protected override async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E, D>(SystemName exclude, DateTime after) {
    await using var db = getdb();
    return (await db.Set<D>()
        .Join(db.Set<CoreStorageMeta.Dto>(), d => d.CoreId, m => m.CoreId, (e, m) => new { CoreEntity=e, Meta=m })
        .Where(dtos => dtos.Meta.DateUpdated > after && dtos.Meta.LastUpdateSystem != exclude.Value)
        .ToListAsync())
        .Select(dto => new CoreEntityAndMeta(dto.CoreEntity.ToBase(), dto.Meta.ToBase()))
        .ToList();
  }
  
  protected override async Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : class {
    await using var db = getdb();
    return (await db.Set<D>().SingleAsync(dto => dto.CoreId == coreid.Value)).ToBase();
  }
}