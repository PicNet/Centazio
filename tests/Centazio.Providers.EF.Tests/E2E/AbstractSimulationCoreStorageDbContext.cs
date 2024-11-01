using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.E2E;

public abstract class AbstractSimulationCoreStorageDbContext(string coreschema, string ctlschema) : CentazioDbContext {
  
  public string CoreSchemaName { get; } = coreschema;
  public string CtlSchemaName { get; } = ctlschema;
  
  public string CoreStorageMetaName { get; } = nameof(CoreStorageMeta).ToLower();
  public string CoreMembershipTypeName { get; } = nameof(CoreMembershipType).ToLower();
  public string CoreCustomerName { get; } = nameof(CoreCustomer).ToLower();
  public string CoreInvoiceName { get; } = nameof(CoreInvoice).ToLower();
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
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
}