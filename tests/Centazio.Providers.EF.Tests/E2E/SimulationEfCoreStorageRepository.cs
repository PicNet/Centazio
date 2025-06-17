using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.E2E;

public class SimulationEfCoreStorageRepository(Func<CentazioDbContext> getdb, IEpochTracker tracker, IDbFieldsHelper dbf) : 
    AbstractCoreStorageEfRepository(getdb), ISimulationCoreStorageRepository {
  
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
    OnEntityAdd += (_, args) => tracker.Add(args.Entity);
    OnEntityUpdate += (_, args) => tracker.Update(args.Entity);
    
    return await UseDb(async db => {
      await DropTablesImpl(db);
      
      await db.ExecSql(dbf.GenerateCreateTableScript(CtlSchemaName, CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
      
      await db.ExecSql(dbf.GenerateCreateTableScript(CoreSchemaName, CoreMembershipTypeName, dbf.GetDbFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
      await db.ExecSql(dbf.GenerateCreateTableScript(CoreSchemaName, CoreCustomerName, dbf.GetDbFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)],
          [], [new ForeignKey([nameof(CoreCustomer.MembershipCoreId)], CoreSchemaName, CoreMembershipTypeName, [nameof(ICoreEntity.CoreId)])]));
      await db.ExecSql(dbf.GenerateCreateTableScript(CoreSchemaName, CoreInvoiceName, dbf.GetDbFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)],
          [], [new ForeignKey([nameof(CoreInvoice.CustomerCoreId)], CoreSchemaName, CoreCustomerName, [nameof(ICoreEntity.CoreId)])]));
      return this;
    });
  }
  
  public override async ValueTask DisposeAsync() {
    await UseDb(async db => {
      await DropTablesImpl(db);
      await base.DisposeAsync();
      return Task.CompletedTask;
    });
  }

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    if (coretype == CoreEntityTypeName.From<CoreMembershipType>()) return  await Impl<CoreMembershipType, CoreMembershipType.Dto>();
    if (coretype == CoreEntityTypeName.From<CoreCustomer>()) return  await Impl<CoreCustomer, CoreCustomer.Dto>();
    if (coretype == CoreEntityTypeName.From<CoreInvoice>()) return  await Impl<CoreInvoice, CoreInvoice.Dto>();
    
    throw new NotSupportedException(coretype.Value);

    async Task<List<ICoreEntity>> Impl<E, D>() where E : CoreEntityBase where D : class, ICoreEntityDto<E> =>
        await UseDb(async db => (await db.Set<D>().Where(e => strids.Contains(e.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList());
  }

  private async Task DropTablesImpl(CentazioDbContext db) {
    await db.ExecSql(dbf.GenerateDropTableScript(CoreSchemaName, CoreInvoiceName));
    await db.ExecSql(dbf.GenerateDropTableScript(CoreSchemaName, CoreCustomerName));
    await db.ExecSql(dbf.GenerateDropTableScript(CoreSchemaName, CoreMembershipTypeName));
    
    await db.ExecSql(dbf.GenerateDropTableScript(CtlSchemaName, CoreStorageMetaName));
  }

  protected async Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    return await UseDb(async db => 
        (await db.Set<D>().SingleAsync(dto => dto.CoreId == coreid.Value)).ToBase());
  }

  protected async Task<List<E>> GetAll<E, D>() where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    return await UseDb(async db => 
        (await db.Set<D>().ToListAsync()).Select(e => e.ToBase()).ToList());
  }

  // ISimulationCoreStorageRepository
  
  public async Task<List<CoreMembershipType>> GetMembershipTypes() => await GetAll<CoreMembershipType, CoreMembershipType.Dto>();
  public async Task<List<CoreCustomer>> GetCustomers() => await GetAll<CoreCustomer, CoreCustomer.Dto>();
  public async Task<List<CoreInvoice>> GetInvoices() => await GetAll<CoreInvoice, CoreInvoice.Dto>();
  
  public async Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid) => await GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);

}