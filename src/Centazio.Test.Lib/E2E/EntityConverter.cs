namespace Centazio.Test.Lib.E2E;

public class EntityConverter(ICtlRepository ctl) {
  
  private int ceidcounter;
  private readonly Dictionary<SystemEntityId, CoreEntityId> systocoreids = [];
  private readonly Dictionary<CoreEntityId, SystemEntityId> coretosysids = [];
  
  public CoreEntityId NewCoreEntityId<T>(SystemName system, SystemEntityId systemid) where T : ICoreEntity {
    var coreid = new CoreEntityId($"{system}/{typeof(T).Name}[{++ceidcounter}]");
    systocoreids[systemid] = coreid;
    coretosysids[coreid] = systemid;
    return coreid;
  }

  public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c, CoreCustomer? existing) => 
      existing is null 
          ? new(NewCoreEntityId<CoreCustomer>(SC.Crm.SYSTEM_NAME, c.SystemId), c.CorrelationId, c.Name, systocoreids[c.MembershipTypeSystemId])
          : existing with { Name = c.Name, MembershipCoreId = systocoreids[c.MembershipTypeSystemId] };

  public async Task<CoreInvoice> CrmInvoiceToCoreInvoice(CrmInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) { 
    custcoreid ??= (await ctl.GetMapsFromSystemIds(SC.Crm.SYSTEM_NAME, CoreEntityTypeName.From<CoreCustomer>(), [i.CustomerSystemId])).Single().CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SC.Crm.SYSTEM_NAME, i.SystemId), i.CorrelationId, custcoreid, i.AmountCents, i.DueDate, i.PaidDate)
        : existing with { Cents = i.AmountCents, DueDate = i.DueDate, PaidDate = i.PaidDate };
  }
  
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a, CoreCustomer? existing) => 
      existing is null 
          ? new CoreCustomer(NewCoreEntityId<CoreCustomer>(SC.Fin.SYSTEM_NAME, a.SystemId), a.CorrelationId, a.Name, systocoreids[new(SC.Crm.PENDING_MEMBERSHIP_TYPE_ID.ToString())])
          : existing with { Name = a.Name };

  public async Task<CoreInvoice> FinInvoiceToCoreInvoice(FinInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= (await ctl.GetMapsFromSystemIds(SC.Fin.SYSTEM_NAME, CoreEntityTypeName.From<CoreCustomer>(), [i.AccountSystemId])).Single().CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SC.Fin.SYSTEM_NAME, i.SystemId), i.CorrelationId, custcoreid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate) 
        : existing with { Cents = (int)(i.Amount * 100), DueDate = DateOnly.FromDateTime(i.DueDate), PaidDate = i.PaidDate };
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m, CoreMembershipType? existing) => 
      existing is null 
          ? new(NewCoreEntityId<CoreMembershipType>(SC.Crm.SYSTEM_NAME, m.SystemId), m.CorrelationId, m.Name)
          : existing with { Name = m.Name };

  public CrmMembershipType CoreMembershipTypeToCrmMembershipType(SystemEntityId systemid, CoreMembershipType m) => new(systemid, m.CorrelationId, UtcDate.UtcNow, m.Name);
  public CrmCustomer CoreCustomerToCrmCustomer(SystemEntityId systemid, CoreCustomer c) => new(systemid, c.CorrelationId, UtcDate.UtcNow, coretosysids[c.MembershipCoreId], c.Name);

  public CrmInvoice CoreInvoiceToCrmInvoice(SystemEntityId systemid, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> custmaps) => 
      new(systemid, i.CorrelationId, UtcDate.UtcNow, custmaps[i.CustomerCoreId], i.Cents, i.DueDate, i.PaidDate);
  
  public FinAccount CoreCustomerToFinAccount(SystemEntityId systemid, CoreCustomer c) => new(systemid, c.CorrelationId, c.Name, UtcDate.UtcNow);
  
  public FinInvoice CoreInvoiceToFinInvoice(SystemEntityId systemid, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> accmaps) => 
      new(systemid, i.CorrelationId, accmaps[i.CustomerCoreId], i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), i.PaidDate);
  
}