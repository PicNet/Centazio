using System.Text.Json;
using Centazio.Core;
using Serilog;

namespace Centazio.E2E.Tests.Systems.Fin;

public interface IFinSystemApi {
  Task<List<string>> GetAccounts(DateTime after);
  Task<List<string>> GetInvoices(DateTime after);
}

public class FinSystem : IFinSystemApi, ISystem {

  private static readonly Random rng = Random.Shared;
  
  internal List<FinAccount> Accounts { get; } = new();
  internal List<FinInvoice> Invoices { get; } = new();
  
  public ISimulation Simulation { get; }
  public FinSystem() => Simulation = new SimulationImpl(Accounts, Invoices);
  
  public Task<List<string>> GetAccounts(DateTime after) => Task.FromResult(Accounts.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  public Task<List<string>> GetInvoices(DateTime after) => Task.FromResult(Invoices.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  // WriteFunction endpoints
  public Task<List<FinAccount>> CreateAccounts(List<FinAccount> news) { 
    var created = news.Select(c => c with { Id = rng.Next(Int32.MaxValue), Updated = UtcDate.UtcNow }).ToList();
    Accounts.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinAccount>> UpdateAccounts(List<FinAccount> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = Accounts.FindIndex(c2 => c2.Id == c.Id);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return Accounts[idx] = update;
    }).ToList());
  }

  public Task<List<FinInvoice>> CreateInvoices(List<FinInvoice> news) {
    var created = news.Select(i => i with { Id = rng.Next(Int32.MaxValue), Updated = UtcDate.UtcNow }).ToList();
    Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinInvoice>> UpdateInvoices(List<FinInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = Invoices.FindIndex(i2 => i2.Id == i.Id);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return Invoices[idx] = update;
    }).ToList());
  }

  public class SimulationImpl(List<FinAccount> accounts, List<FinInvoice> invoices) : ISimulation {
    private const int MAX_NEW_ACCOUNTS = 10;
    private const int MAX_EDIT_ACCOUNTS = 5;
    private const int MAX_NEW_INVOICES = 10;
    private const int MAX_EDIT_INVOICES = 5;

    public void Step() {
      AddAccounts();
      EditAccounts();
      AddInvoices();
      EditInvoices();
    }
    
    private void AddAccounts() {
      var count = rng.Next(MAX_NEW_ACCOUNTS);
      Log.Debug($"FinSimulation - AddAccounts count[{count}]");
      accounts.AddRange(Enumerable.Range(0, count).Select(_ => new FinAccount(rng.Next(Int32.MaxValue), Guid.NewGuid().ToString(), UtcDate.UtcNow)));
    }

    private void EditAccounts() {
      if (!accounts.Any()) return;
      var count = rng.Next(MAX_EDIT_ACCOUNTS);
      Log.Debug($"FinSimulation - EditAccounts count[{count}]");
      Enumerable.Range(0, count).ForEach(_ => {
        var idx = rng.Next(accounts.Count);
        accounts[idx] = accounts[idx] with { Name = Guid.NewGuid().ToString(), Updated = UtcDate.UtcNow };
      });
    }

    private void AddInvoices() {
      var count = rng.Next(MAX_NEW_INVOICES);
      Log.Debug($"FinSimulation - AddInvoices count[{count}]");
      Enumerable.Range(0, count).ForEach(_ => 
          invoices.Add(new FinInvoice(rng.Next(Int32.MaxValue), RandomItem(accounts).Id, rng.Next(100, 10000) / 100.0m, UtcDate.UtcNow, UtcDate.UtcToday.AddDays(rng.Next(-10, 60)), null)));
    }

    private void EditInvoices() {
      if (!invoices.Any()) return;
      var count = rng.Next(MAX_EDIT_INVOICES);
      Log.Debug($"FinSimulation - EditInvoices count[{count}]");
      Enumerable.Range(0, count).ForEach(_ => {
        var idx = rng.Next(invoices.Count);
        invoices[idx] = invoices[idx] with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), Amount = rng.Next(100, 10000) / 100.0m, Updated = UtcDate.UtcNow };
      });
    }

    private T RandomItem<T>(List<T> lst) => lst[rng.Next(lst.Count)];
  }
}

public record FinInvoice(int Id, int AccountId, decimal Amount, DateTime Updated, DateTime DueDate, DateTime? PaidDate);
public record FinAccount(int Id, string Name, DateTime Updated);