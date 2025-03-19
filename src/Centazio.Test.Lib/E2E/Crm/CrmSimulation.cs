namespace Centazio.Test.Lib.E2E.Crm;

public class CrmSimulation(SimulationCtx ctx, CrmApi api) {
    
  public List<CrmCustomer> AddedCustomers { get; private set; } = [];
  public List<CrmCustomer> EditedCustomers { get; private set; } = [];
  public List<CrmInvoice> AddedInvoices { get; private set; } = [];
  public List<CrmInvoice> EditedInvoices { get; private set; } = [];
  public List<CrmMembershipType> EditedMemberships { get; private set; } = [];

  public void Step() {
    AddedCustomers = AddCustomers();
    EditedCustomers = EditCustomers();
    AddedInvoices = AddInvoices();
    EditedInvoices = EditInvoices();
    EditedMemberships = EditMemberships();
  }
  
  private List<CrmCustomer> AddCustomers() {
    var count = Rng.Next(SimulationConstants.CRM_MAX_NEW_CUSTOMERS);
    if (count == 0) return [];
    
    var toadd = Enumerable.Range(0, count)
        .Select(idx => new CrmCustomer(ctx.NewGuiSeid(), UtcDate.UtcNow, Rng.RandomItem(api.MembershipTypes).SystemId, ctx.NewName(nameof(CrmCustomer), api.Customers, idx)))
        .ToList();
    ctx.Debug($"CrmSimulation - AddCustomers[{count}]", toadd.Select(a => $"{a.Name}({a.SystemId})").ToList());
    api.Customers.AddRange(toadd);
    return toadd;
  }

  private List<CrmCustomer> EditCustomers() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, api.Customers.Count), SimulationConstants.CRM_MAX_EDIT_CUSTOMERS);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<CrmCustomer>();
    idxs.ForEach(idx => {
      var cust = api.Customers[idx];
      var (name, newname, oldmt, newmt) = (cust.Name, ctx.UpdateName(cust.Name), cust.MembershipTypeId, Rng.RandomItem(api.MembershipTypes).SystemId);
      var newcust = cust with { MembershipTypeId = newmt, Name = newname, Updated = UtcDate.UtcNow };
      var oldcs = ctx.ChecksumAlg.Checksum(cust);
      var newcs = ctx.ChecksumAlg.Checksum(newcust);
      log.Add($"{cust.SystemId}: Name[{name}->{newname}] Membership[{oldmt}->{newmt}] CS Changed[{oldcs != newcs}]");
      if (oldcs != newcs) api.Customers[idx] = edited.AddAndReturn(newcust);
    });
    ctx.Debug($"CrmSimulation - EditCustomers[{edited.Count}]", log);
    return edited;
  }

  private List<CrmInvoice> AddInvoices() {
    var count = Rng.Next(SimulationConstants.CRM_MAX_NEW_INVOICES);
    if (!api.Customers.Any() || count == 0) return [];
    
    var toadd = new List<CrmInvoice>();
    Enumerable.Range(0, count).ForEach(_ => 
        toadd.Add(new CrmInvoice(ctx.NewGuiSeid(), UtcDate.UtcNow, Rng.RandomItem(api.Customers).SystemId, Rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(Rng.Next(-10, 60))))));
    
    ctx.Debug($"CrmSimulation - AddInvoices[{count}]", toadd.Select(i => {
      var cust = api.Customers.Single(c => c.SystemId == i.CustomerSystemId) as IHasDisplayName;
      return $"Cust:{cust.GetShortDisplayName()}/Inv:{i.SystemId}:{i.AmountCents}c";
    }).ToList());
    
    api.Invoices.AddRange(toadd);
    return toadd.ToList();
  }

  private List<CrmInvoice> EditInvoices() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, api.Invoices.Count), SimulationConstants.CRM_MAX_EDIT_INVOICES);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<CrmInvoice>();
    idxs.ForEach(idx => {
      var newamt = Rng.Next(100, 10000);
      var inv = api.Invoices[idx];
      // lets not edit previously added entities, makes it hard to verify
      if (AddedInvoices.Contains(inv)) return; 
      edited.Add(inv);
      log.Add($"Cust:{inv.CustomerId}({inv.SystemId}) {inv.AmountCents}c -> {newamt}c)");
      api.Invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(Rng.Next(-5, 120)), AmountCents = newamt, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"CrmSimulation - EditInvoices[{edited.Count}]", log);
    return edited;
  }

  private List<CrmMembershipType> EditMemberships() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, api.MembershipTypes.Count), SimulationConstants.CRM_MAX_EDIT_MEMBERSHIPS).ToList();
    // at Epoch 0 all 4 memberships are added, so lets not edit
    if (ctx.Epoch.Epoch == 0 || !idxs.Any()) return [];
    
    var log = new List<string>();
    idxs.ForEach(idx => {
      var (old, newnm) = (api.MembershipTypes[idx].Name, ctx.UpdateName(api.MembershipTypes[idx].Name));
      log.Add($"{old}->{newnm}({api.MembershipTypes[idx].SystemId})");
      api.MembershipTypes[idx] = api.MembershipTypes[idx] with { Name = newnm, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"CrmSimulation - EditMemberships[{idxs.Count}]", log);
    return idxs.Select(idx => api.MembershipTypes[idx]).ToList();
  }
}