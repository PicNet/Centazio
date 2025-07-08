namespace Centazio.Test.Lib.E2E.Sim;

public class CrmSimulation(SimulationCtx ctx, CrmDb db) {
  
  private static readonly int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  private static readonly int CRM_MAX_NEW_CUSTOMERS = 4;
  private static readonly int CRM_MAX_EDIT_CUSTOMERS = 4;
  private static readonly int CRM_MAX_NEW_INVOICES = 4;
  private static readonly int CRM_MAX_EDIT_INVOICES = 4;
  
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
    var count = Rng.Next(CRM_MAX_NEW_CUSTOMERS);
    if (count == 0) return [];
    
    var toadd = Enumerable.Range(0, count)
        .Select(idx => {
          var sysid = ctx.NewGuidSeid();
          return new CrmCustomer(sysid, CorrelationId.Build(SC.CRM_SYSTEM, sysid), UtcDate.UtcNow, Rng.RandomItem(db.MembershipTypes).SystemId, ctx.NewName(nameof(CrmCustomer), db.Customers, idx));
        })
        .ToList();
    ctx.Debug($"CrmSimulation - AddCustomers[{count}]", toadd.Select(a => $"{a.Name}({a.SystemId})").ToList());
    db.Customers.AddRange(toadd);
    return toadd;
  }

  private List<CrmCustomer> EditCustomers() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, db.Customers.Count), CRM_MAX_EDIT_CUSTOMERS);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<CrmCustomer>();
    idxs.ForEach(idx => {
      var cust = db.Customers[idx];
      var (name, newname, oldmt, newmt) = (cust.Name, ctx.UpdateName(cust.Name), cust.MembershipTypeId, Rng.RandomItem(db.MembershipTypes).SystemId);
      var newcust = cust with { MembershipTypeId = newmt, Name = newname, Updated = UtcDate.UtcNow };
      var oldcs = ctx.ChecksumAlg.Checksum(cust);
      var newcs = ctx.ChecksumAlg.Checksum(newcust);
      log.Add($"{cust.SystemId}: Name[{name}->{newname}] Membership[{oldmt}->{newmt}] CS Changed[{oldcs != newcs}]");
      if (oldcs != newcs) db.Customers[idx] = edited.AddAndReturn(newcust);
    });
    ctx.Debug($"CrmSimulation - EditCustomers[{edited.Count}]", log);
    return edited;
  }

  private List<CrmInvoice> AddInvoices() {
    var count = Rng.Next(CRM_MAX_NEW_INVOICES);
    if (!db.Customers.Any() || count == 0) return [];
    
    var toadd = new List<CrmInvoice>();
    Enumerable.Range(0, count).ForEach(_ => {
      var sysid = ctx.NewGuidSeid();
      toadd.Add(new CrmInvoice(sysid, CorrelationId.Build(SC.CRM_SYSTEM, sysid), UtcDate.UtcNow, Rng.RandomItem(db.Customers).SystemId, Rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(Rng.Next(-10, 60)))));
    });
    
    ctx.Debug($"CrmSimulation - AddInvoices[{count}]", toadd.Select(i => {
      var cust = db.Customers.Single(c => c.SystemId == i.CustomerSystemId) as IHasDisplayName;
      return $"Cust:{cust.GetShortDisplayName()}/Inv:{i.SystemId}:{i.AmountCents}c";
    }).ToList());
    
    db.Invoices.AddRange(toadd);
    return toadd.ToList();
  }

  private List<CrmInvoice> EditInvoices() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, db.Invoices.Count), CRM_MAX_EDIT_INVOICES);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<CrmInvoice>();
    idxs.ForEach(idx => {
      var newamt = Rng.Next(100, 10000);
      var inv = db.Invoices[idx];
      // lets not edit previously added entities, makes it hard to verify
      if (AddedInvoices.Contains(inv)) return; 
      edited.Add(inv);
      log.Add($"Cust:{inv.CustomerId}({inv.SystemId}) {inv.AmountCents}c -> {newamt}c)");
      db.Invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(Rng.Next(-5, 120)), AmountCents = newamt, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"CrmSimulation - EditInvoices[{edited.Count}]", log);
    return edited;
  }

  private List<CrmMembershipType> EditMemberships() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, db.MembershipTypes.Count), CRM_MAX_EDIT_MEMBERSHIPS).ToList();
    // at Epoch 0 all 4 memberships are added, so lets not edit
    if (ctx.Epoch.Epoch == 0 || !idxs.Any()) return [];
    
    var log = new List<string>();
    idxs.ForEach(idx => {
      var (old, newnm) = (db.MembershipTypes[idx].Name, ctx.UpdateName(db.MembershipTypes[idx].Name));
      log.Add($"{old}->{newnm}({db.MembershipTypes[idx].SystemId})");
      db.MembershipTypes[idx] = db.MembershipTypes[idx] with { Name = newnm, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"CrmSimulation - EditMemberships[{idxs.Count}]", log);
    return idxs.Select(idx => db.MembershipTypes[idx]).ToList();
  }
}