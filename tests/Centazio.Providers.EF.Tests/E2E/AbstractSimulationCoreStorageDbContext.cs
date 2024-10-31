using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.E2E;

public abstract class AbstractSimulationCoreStorageDbContext(string schema) : CentazioDbContext {
  
  public string SchemaName { get; } = schema;
  public string CoreMembershipTypeName { get; } = nameof(CoreMembershipType).ToLower();
  public string CoreCustomerName { get; } = nameof(CoreCustomer).ToLower();
  public string CoreInvoiceName { get; } = nameof(CoreInvoice).ToLower();
  public string CoreStorageMetaName { get; } = nameof(CoreStorageMeta).ToLower();
  
  public DbSet<CoreStorageMeta.Dto> Metas { get; set; } 
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(SchemaName)
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
      })
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName);
        e.HasKey(e2 => e2.CoreId);
      });
}