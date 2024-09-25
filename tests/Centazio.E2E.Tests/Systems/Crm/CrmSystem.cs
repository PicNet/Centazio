using System.Text.Json;
using Centazio.Core;

namespace Centazio.E2E.Tests.Systems.Crm;

public interface ICrmSystemApi {
  Task<List<string>> GetMembershipTypes(DateTime after);
  Task<List<string>> GetCustomers(DateTime after);
  Task<List<string>> GetInvoices(DateTime after);
}

public class CrmSystem : ICrmSystemApi, ISystem {
  
  private List<CMembershipType> MembershipTypes { get; } = [
    new(Guid.NewGuid(), UtcDate.UtcNow, "Standard:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Silver:0"),
    new(Guid.NewGuid(), UtcDate.UtcNow, "Gold:0")
  ];
  private List<CCustomer> Customers { get; } = new();
  
  private readonly Simulation sim;
  
  public CrmSystem() => sim = new Simulation(Customers, MembershipTypes);
  
  public void Step() => sim.Step();
  
  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(MembershipTypes.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(Customers.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(Customers.SelectMany(e => e.Invoices).Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  class Simulation(List<CCustomer> customers, List<CMembershipType> types) {
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
        .Select(_ => new CCustomer(Guid.NewGuid(), UtcDate.UtcNow, RandomItem(types), [], Guid.NewGuid().ToString())));

    private void EditCustomers() => Enumerable.Range(0, rng.Next(MAX_EDIT_CUSTOMERS)).ForEach(_ => {
      var idx = rng.Next(customers.Count);
      customers[idx] = customers[idx] with { Membership = RandomItem(types), Name = Guid.NewGuid().ToString() };
    });
    
    private void AddInvoices() => Enumerable.Range(0, rng.Next(MAX_NEW_INVOICES)).ForEach(_ => {
      var idx = rng.Next(customers.Count);
      customers[idx].Invoices.Add(new CInvoice(Guid.NewGuid(), UtcDate.UtcNow, customers[idx].Id, rng.Next(100, 10000), DateOnly.FromDateTime(UtcDate.UtcToday.AddDays(rng.Next(-10, 60)))));
    });

    private void EditInvoices() => Enumerable.Range(0, rng.Next(MAX_EDIT_INVOICES)).ForEach(_ => {
      for (var i = 0; i < 3; i++) {
        var idx = rng.Next(customers.Count);
        if (!customers[idx].Invoices.Any()) continue;
        var idx2 = rng.Next(customers[idx].Invoices.Count);
        customers[idx].Invoices[idx2] = customers[idx].Invoices[idx2] with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), AmountCents = rng.Next(100, 10000) };
        break;
      }
    });
    
    private void EditMemberships() => Enumerable.Range(0, rng.Next(MAX_EDIT_MEMBERSHIPS)).ForEach(_ => {
      var idx = rng.Next(types.Count);
      var old = types[idx].Name.Split(':');
      types[idx] = types[idx] with { Name = $"{old[0]}:{Int32.Parse(old[1]) + 1}" };
    });
    
    private T RandomItem<T>(List<T> lst) => lst[rng.Next(lst.Count)];
  }
}


public record CMembershipType(Guid Id, DateTime Updated, string Name);
public record CInvoice(Guid Id, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null);
public record CCustomer(Guid Id, DateTime Updated, CMembershipType Membership, List<CInvoice> Invoices, string Name) {}