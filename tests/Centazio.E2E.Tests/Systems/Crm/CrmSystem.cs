using System.Text.Json;
using Centazio.Core;
using Centazio.Core.Write;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmSystem {
  
  internal static Guid PENDING_MEMBERSHIP_TYPE_ID = Guid.NewGuid();
  internal List<CrmMembershipType> MembershipTypes { get; } = [
    new(PENDING_MEMBERSHIP_TYPE_ID, UtcDate.UtcNow, "Pending:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Standard:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Silver:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Gold:0")
  ];
  internal List<CrmCustomer> Customers { get; } = new();
  internal List<CrmInvoice> Invoices { get; } = new();
  
  public SimulationImpl Simulation { get; }
  
  public CrmSystem(SimulationCtx ctx) => Simulation = new SimulationImpl(ctx, MembershipTypes, Customers, Invoices);

  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(MembershipTypes.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(Customers.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(Invoices.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());

  // WriteFunction endpoints
  public Task<List<CrmCustomer>> CreateCustomers(List<CrmCustomer> news) { 
    var created = news.Select(c => c with { ExternalId = Guid.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
    Customers.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmCustomer>> UpdateCustomers(List<CrmCustomer> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = Customers.FindIndex(c2 => c2.Id == c.Id);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return Customers[idx] = update;
    }).ToList());
  }

  public Task<List<CrmInvoice>> CreateInvoices(List<CrmInvoice> news) {
    var created = news.Select(i => i with { ExternalId = Guid.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
    Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmInvoice>> UpdateInvoices(List<CrmInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = Invoices.FindIndex(i2 => i2.Id == i.Id);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return Invoices[idx] = update;
    }).ToList());
  }
  
  public class SimulationImpl(SimulationCtx ctx, List<CrmMembershipType> types, List<CrmCustomer> customers, List<CrmInvoice> invoices) {
    
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
      var count = ctx.rng.Next(ctx.CRM_MAX_NEW_CUSTOMERS);
      if (count == 0) return [];
      
      var toadd = Enumerable.Range(0, count)
          .Select(idx => new CrmCustomer(Guid.NewGuid(), UtcDate.UtcNow, ctx.RandomItem(types).ExternalId, ctx.NewName(nameof(CrmCustomer), customers, idx)))
          .ToList();
      ctx.Debug($"CrmSimulation - AddCustomers[{count}] - {String.Join(',', toadd.Select(a => $"{a.Name}({a.Id})"))}");
      customers.AddRange(toadd);
      return toadd;
    }

    private List<CrmCustomer> EditCustomers() {
      var idxs = ctx.ShuffleAndTake(Enumerable.Range(0, customers.Count), ctx.CRM_MAX_EDIT_CUSTOMERS);
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      var edited = new List<CrmCustomer>();
      idxs.ForEach(idx => {
        var cust = customers[idx];
        // lets not edit previously added entities, makes it hard to verify
        if (AddedCustomers.Contains(cust)) return;
        // todo: add function `Ctx.Checksum` that takes in ICore/IExternalEntity and calls GetChecksumSubset automatically
        var (name, newname, oldmt, newmt) = (cust.Name, ctx.UpdateName(cust.Name), cust.MembershipTypeId, ctx.RandomItem(types).ExternalId);
        var newcust = cust with { MembershipTypeId = newmt, Name = newname, Updated = UtcDate.UtcNow };
        var oldcs = ctx.checksum.Checksum(cust.GetChecksumSubset());
        var newcs = ctx.checksum.Checksum(newcust.GetChecksumSubset());
        log.Add($"Id[{cust.Id}] Name[{name}->{newname}] Membership[{oldmt}->{newmt}] Checksum[{oldcs}->{newcs}]");
        if (oldcs != newcs) customers[idx] = edited.AddAndReturn(newcust);
      });
      ctx.Debug($"CrmSimulation - EditCustomers[{edited.Count}] - {String.Join(',', log)}");
      return edited;
    }

    private List<CrmInvoice> AddInvoices() {
      if (!ctx.ALLOW_BIDIRECTIONAL) return [];
      
      var count = ctx.rng.Next(ctx.CRM_MAX_NEW_INVOICES);
      if (!customers.Any() || count == 0) return [];
      
      var toadd = new List<CrmInvoice>();
      Enumerable.Range(0, count).ForEach(_ => 
          toadd.Add(new CrmInvoice(Guid.NewGuid(), UtcDate.UtcNow, ctx.RandomItem(customers).ExternalId, ctx.rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(ctx.rng.Next(-10, 60))))));
      ctx.Debug($"CrmSimulation - AddInvoices[{count}] - {String.Join(',', toadd.Select(i => $"Cust:{i.CustomerId}({i.Id}) {i.AmountCents}c"))}");
      invoices.AddRange(toadd);
      return toadd.ToList();
    }

    private List<CrmInvoice> EditInvoices() {
      if (!ctx.ALLOW_BIDIRECTIONAL) return [];
      
      var idxs = ctx.ShuffleAndTake(Enumerable.Range(0, invoices.Count), ctx.CRM_MAX_EDIT_INVOICES);
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      var edited = new List<CrmInvoice>();
      idxs.ForEach(idx => {
        var newamt = ctx.rng.Next(100, 10000);
        var inv = invoices[idx];
        // lets not edit previously added entities, makes it hard to verify
        if (AddedInvoices.Contains(inv)) return; 
        edited.Add(inv);
        log.Add($"Cust:{inv.CustomerId}({inv.Id}) {inv.AmountCents}c -> {newamt}c)");
        invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(ctx.rng.Next(-5, 120)), AmountCents = newamt, Updated = UtcDate.UtcNow };
      });
      ctx.Debug($"CrmSimulation - EditInvoices[{edited.Count}] - {String.Join(',', log)}");
      return edited;
    }

    private List<CrmMembershipType> EditMemberships() {
      var idxs = ctx.ShuffleAndTake(Enumerable.Range(0, types.Count), ctx.CRM_MAX_EDIT_MEMBERSHIPS).ToList();
      // at Epoch 0 all 4 memberships are added, so lets not edit
      if (ctx.Epoch.Epoch == 0 || !idxs.Any()) return [];
      
      var log = new List<string>();
      idxs.ForEach(idx => {
        var (old, newnm) = (types[idx].Name, ctx.UpdateName(types[idx].Name));
        log.Add($"{old}->{newnm}({types[idx].Id})");
        types[idx] = types[idx] with { Name = newnm, Updated = UtcDate.UtcNow };
      });
      ctx.Debug($"CrmSimulation - EditMemberships[{idxs.Count}] - {String.Join(',', log)}");
      return idxs.Select(idx => types[idx]).ToList();
    }
  }
}

public record CrmMembershipType(Guid ExternalId, DateTime Updated, string Name) : IExternalEntity {

  public string Id => ExternalId.ToString();
  public string DisplayName { get; } = Name;
  public object GetChecksumSubset() => new { Name };

}

public record CrmInvoice(Guid ExternalId, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null) : IExternalEntity {

  public string Id => ExternalId.ToString();
  public string DisplayName { get; } = $"Cust:{CustomerId}({ExternalId}) {AmountCents}c";
  public object GetChecksumSubset() => new { CustomerId, AmountCents, DueDate, PaidDate };

}

public record CrmCustomer(Guid ExternalId, DateTime Updated, Guid MembershipTypeId, string Name) : IExternalEntity {

  public string Id => ExternalId.ToString();
  public string DisplayName { get; } = Name;
  public object GetChecksumSubset() => new { MembershipTypeId, Name };

}