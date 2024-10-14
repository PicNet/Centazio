using Centazio.Core;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinSimulation(SimulationCtx ctx, List<FinAccount> accounts, List<FinInvoice> invoices) {

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
    var count = Rng.Next(SimulationConstants.FIN_MAX_NEW_ACCOUNTS);
    if (count == 0) return [];
    
    var toadd = Enumerable.Range(0, count).Select(idx => new FinAccount(Rng.Next(Int32.MaxValue), ctx.NewName(nameof(FinAccount), accounts, idx), UtcDate.UtcNow)).ToList();
    ctx.Debug($"FinSimulation - AddAccounts[{count}]:\n\t{String.Join("\n\t", toadd.Select(a => $"{a.Name}({a.SystemId})"))}");
    accounts.AddRange(toadd);
    return toadd.ToList();
  }

  private List<FinAccount> EditAccounts() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, accounts.Count), SimulationConstants.FIN_MAX_EDIT_ACCOUNTS);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<FinAccount>();
    idxs.ForEach(idx => {
      var acc = accounts[idx];
      var (name, newname) = (acc.Name, ctx.UpdateName(acc.Name));
      var newacc = acc with { Name = newname, Updated = UtcDate.UtcNow };
      var oldcs = ctx.ChecksumAlg.Checksum(acc);
      var newcs = ctx.ChecksumAlg.Checksum(newacc);
      log.Add($"Id[{acc.SystemId}] Name[{name}->{newname}] Checksum[{oldcs}->{newcs}]");
      if (oldcs != newcs) accounts[idx] = edited.AddAndReturn(newacc);
    });
    ctx.Debug($"FinSimulation - EditAccounts[{edited.Count}]:\n\t{String.Join("\n\t", log)}");
    return edited;
  }

  private List<FinInvoice> AddInvoices() {
    var count = Rng.Next(SimulationConstants.FIN_MAX_NEW_INVOICES);
    if (!accounts.Any() || count == 0) return [];
    
    var toadd = new List<FinInvoice>();
    Enumerable.Range(0, count).ForEach(_ => toadd.Add(new FinInvoice(Rng.Next(Int32.MaxValue), Rng.RandomItem(accounts).FinAccId, Rng.Next(100, 10000) / 100.0m, UtcDate.UtcNow, UtcDate.UtcToday.AddDays(Rng.Next(-10, 60)), null)));
    ctx.Debug($"FinSimulation - AddInvoices[{count}]:\n\t{String.Join("\n\t", toadd.Select(i => $"Acc:{i.AccountId}({i.SystemId}) ${i.Amount:N2}"))}");
    invoices.AddRange(toadd);
    return toadd;
  }

  private List<FinInvoice> EditInvoices() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, invoices.Count), SimulationConstants.FIN_MAX_EDIT_INVOICES);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<FinInvoice>();
    idxs.ForEach(_ => {
      var idx = Rng.Next(invoices.Count);
      var inv = invoices[idx];
      if (AddedInvoices.Contains(inv)) return;
      edited.Add(inv);
      var newamt = Rng.Next(100, 10000) / 100.0m;
      log.Add($"Acc:{inv.AccountId}({inv.SystemId}) ${inv.Amount:N2}->${newamt:N2}");
      invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(Rng.Next(-5, 120)), Amount = newamt, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"FinSimulation - EditInvoices[{edited.Count}]:\n\t{String.Join("\n\t", log)}");
    return edited;
  }
}