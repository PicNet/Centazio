﻿using System.Text.Json;
using Centazio.Core;

namespace Centazio.E2E.Tests.Systems.Crm;

public interface ICrmSystemApi {
  Task<List<string>> GetMembershipTypes(DateTime after);
  Task<List<string>> GetCustomers(DateTime after);
  Task<List<string>> GetInvoices(DateTime after);
}

public class CrmSystem : ICrmSystemApi, ISystem {
  
  private List<CrmMembershipType> MembershipTypes { get; } = [
    new(Guid.NewGuid(), UtcDate.UtcNow, "Standard:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Silver:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Gold:0")
  ];
  private List<CrmCustomer> Customers { get; } = new();
  private List<CrmInvoice> Invoices { get; } = new();
  
  private readonly Simulation sim;
  
  public CrmSystem() => sim = new Simulation(MembershipTypes, Customers, Invoices);
  
  public void Step() => sim.Step();
  
  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(MembershipTypes.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(Customers.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(Invoices.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  class Simulation(List<CrmMembershipType> types, List<CrmCustomer> customers, List<CrmInvoice> invoices) {
    private const int MAX_NEW_CUSTOMERS = 10;
    private const int MAX_EDIT_CUSTOMERS = 5;
    private const int MAX_EDIT_MEMBERSHIPS = 2;
    private const int MAX_NEW_INVOICES = 10;
    private const int MAX_EDIT_INVOICES = 5;
    
    private readonly Random rng = Random.Shared;

    public void Step() {
      AddCustomers();
      EditCustomers();
      AddInvoices();
      EditInvoices();
      EditMemberships();
    }
    
    private void AddCustomers() => customers.AddRange(Enumerable.Range(0, rng.Next(MAX_NEW_CUSTOMERS))
        .Select(_ => new CrmCustomer(Guid.NewGuid(), UtcDate.UtcNow, RandomItem(types).Id, Guid.NewGuid().ToString())));

    private void EditCustomers() => Enumerable.Range(0, rng.Next(MAX_EDIT_CUSTOMERS)).ForEach(_ => {
      var idx = rng.Next(customers.Count);
      customers[idx] = customers[idx] with { MembershipTypeId = RandomItem(types).Id, Name = Guid.NewGuid().ToString() };
    });
    
    private void AddInvoices() => 
        Enumerable.Range(0, rng.Next(MAX_NEW_INVOICES))
            .ForEach(_ => invoices.Add(
                new CrmInvoice(Guid.NewGuid(), UtcDate.UtcNow, RandomItem(customers).Id, rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(rng.Next(-10, 60))))));

    private void EditInvoices() => Enumerable.Range(0, rng.Next(MAX_EDIT_INVOICES)).ForEach(_ => {
      var idx = rng.Next(invoices.Count);
      invoices[idx] = invoices[idx] with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), AmountCents = rng.Next(100, 10000) };
    });
    
    private void EditMemberships() => Enumerable.Range(0, rng.Next(MAX_EDIT_MEMBERSHIPS)).ForEach(_ => {
      var idx = rng.Next(types.Count);
      var old = types[idx].Name.Split(':');
      types[idx] = types[idx] with { Name = $"{old[0]}:{Int32.Parse(old[1]) + 1}" };
    });
    
    private T RandomItem<T>(List<T> lst) => lst[rng.Next(lst.Count)];
  }

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
}


public record CrmMembershipType(Guid Id, DateTime Updated, string Name);
public record CrmInvoice(Guid Id, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null);
public record CrmCustomer(Guid Id, DateTime Updated, Guid MembershipTypeId, string Name);