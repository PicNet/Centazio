﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.E2E;

public abstract class AbstractCoreStorageDbContext(string schema) : CentazioDbContext {
  
  public string SchemaName { get; } = schema;
  public string CoreMembershipTypeName { get; } = nameof(CoreMembershipType).ToLower();
  public string CoreCustomerName { get; } = nameof(CoreCustomer).ToLower();
  public string CoreInvoiceName { get; } = nameof(CoreInvoice).ToLower();
  
  protected sealed override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema(SchemaName)
      // todo: do we need `HasKey`
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
  
  public async Task CreateTableIfNotExists(IDbFieldsHelper dbf) {
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, CoreMembershipTypeName, dbf.GetDbFields<CoreMembershipType>(), [nameof(ICoreEntity.CoreId)]));
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, CoreCustomerName, dbf.GetDbFields<CoreCustomer>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreCustomer.MembershipCoreId)}]) REFERENCES [{CoreMembershipTypeName}]([{nameof(ICoreEntity.CoreId)}])");
    await Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(SchemaName, CoreInvoiceName, dbf.GetDbFields<CoreInvoice>(), [nameof(ICoreEntity.CoreId)]),
        $"FOREIGN KEY ([{nameof(CoreInvoice.CustomerCoreId)}]) REFERENCES [{CoreCustomerName}]([{nameof(ICoreEntity.CoreId)}])");
    
  }
  
  public async Task DropTables() {
    #pragma warning disable EF1002
    await Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {CoreInvoiceName}; DROP TABLE IF EXISTS {CoreCustomerName}; DROP TABLE IF EXISTS {CoreMembershipTypeName};");
    #pragma warning restore EF1002
  }

}