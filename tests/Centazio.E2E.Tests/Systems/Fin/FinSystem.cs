using System.Text.Json;
using Centazio.Core;

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

    public void Step() {
      AddAccounts();
      EditAccounts();
      AddInvoices();
      EditInvoices();
    }
    
    private void AddAccounts() {
      var count = rng.Next(SimulationCtx.FIN_MAX_NEW_ACCOUNTS);
      if (count == 0) return;
      
      var toadd = Enumerable.Range(0, count).Select(idx => new FinAccount(rng.Next(Int32.MaxValue), SimulationCtx.NewName(nameof(FinAccount), accounts, idx), UtcDate.UtcNow)).ToList();
      SimulationCtx.Debug($"FinSimulation - AddAccounts[{count}] - {String.Join(',', toadd.Select(a => $"{a.Id}({a.Name})"))}");
      accounts.AddRange(toadd);
    }

    private void EditAccounts() {
      var idxs = Enumerable.Range(0, accounts.Count).Shuffle(SimulationCtx.FIN_MAX_EDIT_ACCOUNTS);
      if (!idxs.Any()) return;
      
      var log = new List<string>();
      idxs.ForEach(idx => {
        var (name, newname) = (accounts[idx].Name, SimulationCtx.UpdateName(accounts[idx].Name));
        log.Add($"{accounts[idx].Id}({name}->{newname})");
        accounts[idx] = accounts[idx] with { Name = newname, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"FinSimulation - EditAccounts[{idxs.Count}] - {String.Join(',', log)}");
    }

    private void AddInvoices() {
      var count = rng.Next(SimulationCtx.FIN_MAX_NEW_INVOICES);
      if (!accounts.Any() || count == 0) return;
      
      var toadd = new List<FinInvoice>();
      Enumerable.Range(0, count).ForEach(_ => toadd.Add(new FinInvoice(rng.Next(Int32.MaxValue), accounts.RandomItem().Id, rng.Next(100, 10000) / 100.0m, UtcDate.UtcNow, UtcDate.UtcToday.AddDays(rng.Next(-10, 60)), null)));
      SimulationCtx.Debug($"FinSimulation - AddInvoices[{count}] - {String.Join(',', toadd.Select(i => $"{i.Id}(${i.Amount:N2})"))}");
      invoices.AddRange(toadd);
    }

    private void EditInvoices() {
      var idxs = Enumerable.Range(0, invoices.Count).Shuffle(SimulationCtx.FIN_MAX_EDIT_INVOICES);
      if (!idxs.Any()) return;
      
      var log = new List<string>();
      idxs.ForEach(_ => {
        var idx = rng.Next(invoices.Count);
        var newamt = rng.Next(100, 10000) / 100.0m;
        log.Add($"{invoices[idx].Id}(${invoices[idx].Amount:N2} -> ${newamt:N2})");
        invoices[idx] = invoices[idx] with { PaidDate = UtcDate.UtcNow.AddDays(rng.Next(-5, 120)), Amount = newamt, Updated = UtcDate.UtcNow };
      });
      SimulationCtx.Debug($"FinSimulation - EditInvoices[{idxs.Count}] - {String.Join(',', log)}");
    }
  }
}

public record FinInvoice(int Id, int AccountId, decimal Amount, DateTime Updated, DateTime DueDate, DateTime? PaidDate);
public record FinAccount(int Id, string Name, DateTime Updated);