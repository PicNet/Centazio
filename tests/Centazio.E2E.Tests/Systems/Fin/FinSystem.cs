﻿using System.Text.Json;
using Centazio.Core;
using Centazio.Core.Write;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinSystem  {

  internal List<FinAccount> Accounts { get; } = new();
  internal List<FinInvoice> Invoices { get; } = new();
  
  private readonly SimulationCtx ctx;
  public SimulationImpl Simulation { get; }
  
  public FinSystem(SimulationCtx ctx) {
    this.ctx = ctx;
    Simulation = new SimulationImpl(ctx, Accounts, Invoices);
  }

  public Task<List<string>> GetAccounts(DateTime after) => Task.FromResult(Accounts.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  public Task<List<string>> GetInvoices(DateTime after) => Task.FromResult(Invoices.Where(e => e.Updated > after).Select(e => JsonSerializer.Serialize(e)).ToList());
  
  // WriteFunction endpoints
  public Task<List<FinAccount>> CreateAccounts(List<FinAccount> news) { 
    var created = news.Select(c => c with { ExternalId = ctx.rng.Next(Int32.MaxValue), Updated = UtcDate.UtcNow }).ToList();
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
    var created = news.Select(i => i with { ExternalId = ctx.rng.Next(Int32.MaxValue), Updated = UtcDate.UtcNow }).ToList();
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

  public class SimulationImpl(SimulationCtx ctx, List<FinAccount> accounts, List<FinInvoice> invoices) {

    public List<FinAccount> AddedAccounts { get; private set; } = [];
    public List<FinAccount> EditedAccounts { get; private set; } = [];
    public List<FinInvoice> AddedInvoices { get; private set; } = [];
    public List<FinInvoice> EditedInvoices { get; private set; } = [];
    
    public void Step() {
      AddedAccounts = AddAccounts();
      EditedAccounts = EditAccounts();
      AddedInvoices = AddInvoices();
      EditedInvoices = EditInvoices();
    }
    
    private List<FinAccount> AddAccounts() {
      var count = ctx.rng.Next(ctx.FIN_MAX_NEW_ACCOUNTS);
      if (!ctx.ALLOW_BIDIRECTIONAL || count == 0) return [];
      
      var toadd = Enumerable.Range(0, count).Select(idx => new FinAccount(ctx.rng.Next(Int32.MaxValue), ctx.NewName(nameof(FinAccount), accounts, idx), UtcDate.UtcNow)).ToList();
      ctx.Debug($"FinSimulation - AddAccounts[{count}] - {String.Join(',', toadd.Select(a => $"{a.Name}({a.Id})"))}");
      accounts.AddRange(toadd);
      return toadd.ToList();
    }

    private List<FinAccount> EditAccounts() {
      var idxs = ctx.ShuffleAndTake(Enumerable.Range(0, accounts.Count), ctx.FIN_MAX_EDIT_ACCOUNTS);
      if (!ctx.ALLOW_BIDIRECTIONAL || !idxs.Any()) return [];
      
      var log = new List<string>();
      var edited = new List<FinAccount>();
      idxs.ForEach(idx => {
        var acc = accounts[idx];
        if (AddedAccounts.Contains(acc)) return;
        var (name, newname) = (acc.Name, ctx.UpdateName(acc.Name));
        var newacc = acc with { Name = newname, Updated = UtcDate.UtcNow };
        var oldcs = ctx.checksum.Checksum(acc.GetChecksumSubset());
        var newcs = ctx.checksum.Checksum(newacc.GetChecksumSubset());
        log.Add($"Id[{acc.Id}] Name[{name}->{newname}] Checksum[{oldcs}->{newcs}]");
        if (oldcs != newcs) accounts[idx] = edited.AddAndReturn(newacc);
      });
      ctx.Debug($"FinSimulation - EditAccounts[{edited.Count}] - {String.Join(',', log)}");
      return edited;
    }

    private List<FinInvoice> AddInvoices() {
      var count = ctx.rng.Next(ctx.FIN_MAX_NEW_INVOICES);
      if (!accounts.Any() || count == 0) return [];
      
      var toadd = new List<FinInvoice>();
      Enumerable.Range(0, count).ForEach(_ => toadd.Add(new FinInvoice(ctx.rng.Next(Int32.MaxValue), ctx.RandomItem(accounts).ExternalId, ctx.rng.Next(100, 10000) / 100.0m, UtcDate.UtcNow, UtcDate.UtcToday.AddDays(ctx.rng.Next(-10, 60)), null)));
      ctx.Debug($"FinSimulation - AddInvoices[{count}] - {String.Join(',', toadd.Select(i => $"Acc:{i.AccountId}({i.Id}) ${i.Amount:N2}"))}");
      invoices.AddRange(toadd);
      return toadd.ToList();
    }

    private List<FinInvoice> EditInvoices() {
      var idxs = ctx.ShuffleAndTake(Enumerable.Range(0, invoices.Count), ctx.FIN_MAX_EDIT_INVOICES);
      if (!idxs.Any()) return [];
      
      var log = new List<string>();
      var edited = new List<FinInvoice>();
      idxs.ForEach(_ => {
        var idx = ctx.rng.Next(invoices.Count);
        var inv = invoices[idx];
        if (AddedInvoices.Contains(inv)) return;
        edited.Add(inv);
        var newamt = ctx.rng.Next(100, 10000) / 100.0m;
        log.Add($"Acc:{inv.AccountId}({inv.Id}) ${inv.Amount:N2}->${newamt:N2}");
        invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(ctx.rng.Next(-5, 120)), Amount = newamt, Updated = UtcDate.UtcNow };
      });
      ctx.Debug($"FinSimulation - EditInvoices[{edited.Count}] - {String.Join(',', log)}");
      return edited;
    }
  }
}

public record FinInvoice(int ExternalId, int AccountId, decimal Amount, DateTime Updated, DateTime DueDate, DateTime? PaidDate) : IExternalEntity {

  public string Id => ExternalId.ToString();
  public string DisplayName { get; } = $"Acct:{AccountId}({ExternalId}) {Amount}c";
  public object GetChecksumSubset() => new { AccountId, Amount, DueDate, PaidDate };

}

public record FinAccount(int ExternalId, string Name, DateTime Updated) : IExternalEntity {

  public string Id => ExternalId.ToString();
  public string DisplayName { get; } = Name;
  public object GetChecksumSubset() => new { Name };

}