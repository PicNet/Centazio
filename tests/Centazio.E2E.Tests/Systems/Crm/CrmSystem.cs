﻿using System.Text.Json;
using Centazio.Core;

namespace Centazio.E2E.Tests.Systems.Crm;

public interface ICrmSystemApi {
  Task<List<string>> GetMembershipTypes(DateTime after);
  Task<List<string>> GetCustomers(DateTime after);
  Task<List<string>> GetInvoices(DateTime after);
}

public class CrmSystem : ICrmSystemApi, ISystem {
  
  internal static Guid PENDING_MEMBERSHIP_TYPE_ID = Guid.NewGuid();
  internal List<CrmMembershipType> MembershipTypes { get; } = [
    new(PENDING_MEMBERSHIP_TYPE_ID, UtcDate.UtcNow, "Pending:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Standard:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Silver:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Gold:0")
  ];
  internal List<CrmCustomer> Customers { get; } = new();
  internal List<CrmInvoice> Invoices { get; } = new();
  
  public ISimulation Simulation { get; }
  
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
  
  class SimulationImpl(List<CrmMembershipType> types, List<CrmCustomer> customers, List<CrmInvoice> invoices) : ISimulation {
    
    
    private readonly Random rng = Random.Shared;

    public void Step() {
      AddCustomers();
      EditCustomers();
      AddInvoices();
      EditInvoices();
      EditMemberships();
    }
    
    private void AddCustomers() {
      var count = rng.Next(SimulationCtx.CRM_MAX_NEW_CUSTOMERS);
      if (count == 0) return;
      
      var toadd = Enumerable.Range(0, count)
          .Select(idx => new CrmCustomer(Guid.NewGuid(), UtcDate.UtcNow, types.RandomItem().Id, SimulationCtx.NewName(nameof(CrmCustomer), customers, idx)))
          .ToList();
      SimulationCtx.Debug($"CrmSimulation - AddCustomers[{count}] - {String.Join(',', toadd.Select(a => a.Name))}");
      customers.AddRange(toadd);
    }

    private void EditCustomers() {
      var count = rng.Next(SimulationCtx.CRM_MAX_EDIT_CUSTOMERS);
      if (!customers.Any() || count == 0) return;
      
      var log = new List<string>();
      Enumerable.Range(0, count).ForEach(_ => {
        var idx = rng.Next(customers.Count);
        var (name, newname) = (customers[idx].Name, SimulationCtx.UpdateName(customers[idx].Name));
        log.Add($"{name}->{newname}");
        customers[idx] = customers[idx] with { MembershipTypeId = types.RandomItem().Id, Name = newname, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditCustomers[{count}] - {String.Join(',', log)}");
    }

    private void AddInvoices() {
      var count = rng.Next(SimulationCtx.CRM_MAX_NEW_INVOICES);
      if (!customers.Any() || count == 0) return;
      
      var toadd = new List<CrmInvoice>();
      Enumerable.Range(0, count).ForEach(_ => 
          toadd.Add(new CrmInvoice(Guid.NewGuid(), UtcDate.UtcNow, customers.RandomItem().Id, rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(rng.Next(-10, 60))))));
      SimulationCtx.Debug($"CrmSimulation - AddInvoices[{count}] - {String.Join(',', toadd.Select(i => $"{i.Id}({i.AmountCents}c)"))}");
      invoices.AddRange(toadd);
    }

    private void EditInvoices() {
      var count = rng.Next(SimulationCtx.CRM_MAX_EDIT_INVOICES);
      if (!invoices.Any() || count == 0) return;
      
      var log = new List<string>();
      Enumerable.Range(0, count).ForEach(_ => {
        var idx = rng.Next(invoices.Count);
        var newamt = rng.Next(100, 10000);
        log.Add($"{invoices[idx].Id}({invoices[idx].AmountCents}c -> {newamt}c)");
        invoices[idx] = invoices[idx] with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), AmountCents = newamt, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditInvoices[{count}] - {String.Join(',', log)}");
    }

    private void EditMemberships() {
      var count = rng.Next(SimulationCtx.CRM_MAX_EDIT_MEMBERSHIPS);
      if (count == 0) return;
      
      var log = new List<string>();
      Enumerable.Range(0, count).ForEach(_ => {
        var idx = rng.Next(types.Count);
        var (old, newnm) = (types[idx].Name, SimulationCtx.UpdateName(types[idx].Name));
        log.Add($"{old} -> {newnm}");
        types[idx] = types[idx] with { Name = newnm, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"CrmSimulation - EditMemberships[{count}] - {String.Join(',', log)}");
    }
  }
}


public record CrmMembershipType(Guid Id, DateTime Updated, string Name);
public record CrmInvoice(Guid Id, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null);
public record CrmCustomer(Guid Id, DateTime Updated, Guid MembershipTypeId, string Name);