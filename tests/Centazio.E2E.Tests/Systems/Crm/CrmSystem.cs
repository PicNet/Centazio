using System.Text.Json;
using Centazio.Core;

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
  
  public CrmSystem() => Simulation = new SimulationImpl(MembershipTypes, Customers, Invoices);
  
  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(MembershipTypes.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(Customers.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(Invoices.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());

  // WriteFunction endpoints
  public Task<List<CrmCustomer>> CreateCustomers(List<CrmCustomer> news) { 
    var created = news.Select(c => c with { Id = Guid.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
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
    var created = news.Select(i => i with { Id = Guid.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
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
  
  public class SimulationImpl(List<CrmMembershipType> types, List<CrmCustomer> customers, List<CrmInvoice> invoices) {
    
    
    private readonly Random rng = Random.Shared;
    
    public List<Guid> AddedCustomers { get; private set; } = [];
    public List<Guid> EditedCustomers { get; private set; } = [];
    public List<Guid> AddedInvoices { get; private set; } = [];
    public List<Guid> EditedInvoices { get; private set; } = [];
    public List<Guid> EditedMemberships { get; private set; } = [];

    public void Step() {
      AddedCustomers = AddCustomers();
      EditedCustomers = EditCustomers();
      AddedInvoices = AddInvoices();
      EditedInvoices = EditInvoices();
      EditedMemberships = EditMemberships();
    }
    
    private List<Guid> AddCustomers() {
      var count = rng.Next(SimulationCtx.CRM_MAX_NEW_CUSTOMERS);
      if (count == 0) return [];
      
      var toadd = Enumerable.Range(0, count)
          .Select(idx => new CrmCustomer(Guid.NewGuid(), UtcDate.UtcNow, types.RandomItem().Id, SimulationCtx.NewName(nameof(CrmCustomer), customers, idx)))
          .ToList();
      SimulationCtx.Debug($"CrmSimulation - AddCustomers[{count}] - {String.Join(',', toadd.Select(a => $"{a.Name}({a.Id})"))}");
      customers.AddRange(toadd);
      return toadd.Select(c => c.Id).ToList();
    }

    private List<Guid> EditCustomers() {
      var idxs = Enumerable.Range(0, customers.Count).Shuffle(SimulationCtx.CRM_MAX_EDIT_CUSTOMERS);
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      idxs.ForEach(idx => {
        var (name, newname) = (customers[idx].Name, SimulationCtx.UpdateName(customers[idx].Name));
        log.Add($"{name}->{newname}({customers[idx].Id})");
        customers[idx] = customers[idx] with { MembershipTypeId = types.RandomItem().Id, Name = newname, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditCustomers[{idxs.Count}] - {String.Join(',', log)}");
      return idxs.Select(idx => customers[idx].Id).Where(id => !AddedCustomers.Contains(id)).ToList();
    }

    private List<Guid> AddInvoices() {
      if (!SimulationCtx.ALLOW_BIDIRECTIONAL) return [];
      
      var count = rng.Next(SimulationCtx.CRM_MAX_NEW_INVOICES);
      if (!customers.Any() || count == 0) return [];
      
      var toadd = new List<CrmInvoice>();
      Enumerable.Range(0, count).ForEach(_ => 
          toadd.Add(new CrmInvoice(Guid.NewGuid(), UtcDate.UtcNow, customers.RandomItem().Id, rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(rng.Next(-10, 60))))));
      SimulationCtx.Debug($"CrmSimulation - AddInvoices[{count}] - {String.Join(',', toadd.Select(i => $"Cust:{i.CustomerId}({i.Id}) {i.AmountCents}c"))}");
      invoices.AddRange(toadd);
      return toadd.Select(e => e.Id).ToList();
    }

    private List<Guid> EditInvoices() {
      if (!SimulationCtx.ALLOW_BIDIRECTIONAL) return [];
      
      var idxs = Enumerable.Range(0, invoices.Count).Shuffle(SimulationCtx.CRM_MAX_EDIT_INVOICES);
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      idxs.ForEach(idx => {
        var newamt = rng.Next(100, 10000);
        var inv = invoices[idx];
        log.Add($"Cust:{inv.CustomerId}({inv.Id}) {inv.AmountCents}c -> {newamt}c)");
        invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), AmountCents = newamt, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditInvoices[{idxs.Count}] - {String.Join(',', log)}");
      return idxs.Select(idx => invoices[idx].Id).Where(id => !AddedInvoices.Contains(id)).ToList();
    }

    private List<Guid> EditMemberships() {
      var idxs = Enumerable.Range(0, types.Count).Shuffle(SimulationCtx.CRM_MAX_EDIT_MEMBERSHIPS).ToList();
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      idxs.ForEach(idx => {
        var (old, newnm) = (types[idx].Name, SimulationCtx.UpdateName(types[idx].Name));
        log.Add($"{old}->{newnm}({types[idx].Id})");
        types[idx] = types[idx] with { Name = newnm, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditMemberships[{idxs.Count}] - {String.Join(',', log)}");
      return idxs.Select(idx => types[idx].Id).ToList();
    }
  }
}

public record CrmMembershipType(Guid Id, DateTime Updated, string Name);
public record CrmInvoice(Guid Id, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null);
public record CrmCustomer(Guid Id, DateTime Updated, Guid MembershipTypeId, string Name);