using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;

namespace Centazio.E2E.Tests;

public class EntityConverter(ICtlRepository ctl) {
  
  private int ceidcounter;
  private readonly Dictionary<SystemEntityId, CoreEntityId> systocoreids = new();
  private readonly Dictionary<CoreEntityId, SystemEntityId> coretosysids = new();
  
  public CoreEntityId NewCoreEntityId<T>(SystemName system, SystemEntityId systemid) where T : ICoreEntity {
    var coreid = new CoreEntityId($"{system}/{typeof(T).Name}[{++ceidcounter}]");
    systocoreids[systemid] = coreid;
    coretosysids[coreid] = systemid;
    return coreid;
  }

  public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c, CoreCustomer? existing) => 
      existing is null 
          ? new(NewCoreEntityId<CoreCustomer>(SimulationConstants.CRM_SYSTEM, c.SystemId), c.SystemId, c.Name, systocoreids[c.MembershipTypeSystemId])
          : existing with { Name = c.Name, MembershipCoreId = systocoreids[c.MembershipTypeSystemId] };

  public async Task<CoreInvoice> CrmInvoiceToCoreInvoice(CrmInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) { 
    custcoreid ??= (await ctl.GetMapsFromSystemIds(SimulationConstants.CRM_SYSTEM, CoreEntityTypeName.From<CoreCustomer>(), [i.CustomerSystemId])).Single().CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SimulationConstants.CRM_SYSTEM, i.SystemId), i.SystemId, custcoreid, i.AmountCents, i.DueDate, i.PaidDate)
        : existing with { Cents = i.AmountCents, DueDate = i.DueDate, PaidDate = i.PaidDate };
  }
  
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a, CoreCustomer? existing) => 
      existing is null 
          ? new CoreCustomer(NewCoreEntityId<CoreCustomer>(SimulationConstants.FIN_SYSTEM, a.SystemId), a.SystemId, a.Name, systocoreids[new(SimulationConstants.PENDING_MEMBERSHIP_TYPE_ID.ToString())])
          : existing with { Name = a.Name };

  public async Task<CoreInvoice> FinInvoiceToCoreInvoice(FinInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= (await ctl.GetMapsFromSystemIds(SimulationConstants.FIN_SYSTEM, CoreEntityTypeName.From<CoreCustomer>(), [i.AccountSystemId])).Single().CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SimulationConstants.FIN_SYSTEM, i.SystemId), i.SystemId, custcoreid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate) 
        : existing with { Cents = (int)(i.Amount * 100), DueDate = DateOnly.FromDateTime(i.DueDate), PaidDate = i.PaidDate };
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m, CoreMembershipType? existing) => 
      existing is null 
          ? new(NewCoreEntityId<CoreMembershipType>(SimulationConstants.CRM_SYSTEM, m.SystemId), m.SystemId, m.Name)
          : existing with { Name = m.Name };

  public CrmMembershipType CoreMembershipTypeToCrmMembershipType(Guid id, CoreMembershipType m) => new(id, UtcDate.UtcNow, m.Name);
  public CrmCustomer CoreCustomerToCrmCustomer(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Parse(coretosysids[c.MembershipCoreId].Value), c.Name);

  public CrmInvoice CoreInvoiceToCrmInvoice(Guid id, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> custmaps) => 
      new(id, UtcDate.UtcNow, Guid.Parse(custmaps[i.CustomerCoreId].Value), i.Cents, i.DueDate, i.PaidDate);
  
  public FinAccount CoreCustomerToFinAccount(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  
  public FinInvoice CoreInvoiceToFinInvoice(int id, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> accmaps) => 
      new(id, Int32.Parse(accmaps[i.CustomerCoreId]), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), i.PaidDate);
  
}