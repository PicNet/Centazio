namespace Centazio.Test.Lib.E2E.Sim;

public class FinSimulation(SimulationCtx ctx, FinDb db) {

  private static readonly int FIN_MAX_NEW_ACCOUNTS = 2;
  private static readonly int FIN_MAX_EDIT_ACCOUNTS = 4;
  private static readonly int FIN_MAX_NEW_INVOICES = 2;
  private static readonly int FIN_MAX_EDIT_INVOICES = 2;
  
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
    var count = Rng.Next(FIN_MAX_NEW_ACCOUNTS);
    if (count == 0) return [];
    
    var sysid = ctx.NewIntSeid();
    var toadd = Enumerable.Range(0, count).Select(idx => new FinAccount(sysid, CorrelationId.Build(SC.FIN_SYSTEM, sysid),  ctx.NewName(nameof(FinAccount), db.Accounts, idx), UtcDate.UtcNow)).ToList();
    ctx.Debug($"FinSimulation - AddAccounts[{count}]", toadd.Select(a => $"{a.Name}({a.SystemId})").ToList());
    db.Accounts.AddRange(toadd);
    return toadd.ToList();
  }

  private List<FinAccount> EditAccounts() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, db.Accounts.Count), FIN_MAX_EDIT_ACCOUNTS);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<FinAccount>();
    idxs.ForEach(idx => {
      var acc = db.Accounts[idx];
      var (name, newname) = (acc.Name, ctx.UpdateName(acc.Name));
      var newacc = acc with { Name = newname, Updated = UtcDate.UtcNow };
      var oldcs = ctx.ChecksumAlg.Checksum(acc);
      var newcs = ctx.ChecksumAlg.Checksum(newacc);
      log.Add($"Id[{acc.SystemId}] Name[{name}->{newname}] Checksum[{oldcs}->{newcs}]");
      if (oldcs != newcs) db.Accounts[idx] = edited.AddAndReturn(newacc);
    });
    ctx.Debug($"FinSimulation - EditAccounts[{edited.Count}]", log);
    return edited;
  }

  private List<FinInvoice> AddInvoices() {
    var count = Rng.Next(FIN_MAX_NEW_INVOICES);
    if (!db.Accounts.Any() || count == 0) return [];
    
    var toadd = new List<FinInvoice>();
    Enumerable.Range(0, count).ForEach(_ => {
      var sysid = ctx.NewIntSeid();
      toadd.Add(new FinInvoice(sysid, CorrelationId.Build(SC.FIN_SYSTEM, sysid), Rng.RandomItem(db.Accounts).SystemId, Rng.Next(100, 10000) / 100.0m, UtcDate.UtcNow, UtcDate.UtcToday.AddDays(Rng.Next(-10, 60)), null));
    });
    ctx.Debug($"FinSimulation - AddInvoices[{count}]", toadd.Select(i => $"Acc:{i.AccountId}({i.SystemId}) ${i.Amount:N2}").ToList());
    db.Invoices.AddRange(toadd);
    return toadd;
  }

  private List<FinInvoice> EditInvoices() {
    var idxs = Rng.ShuffleAndTake(Enumerable.Range(0, db.Invoices.Count), FIN_MAX_EDIT_INVOICES);
    if (!idxs.Any()) return [];
    
    var log = new List<string>();
    var edited = new List<FinInvoice>();
    idxs.ForEach(_ => {
      var idx = Rng.Next(db.Invoices.Count);
      var inv = db.Invoices[idx];
      if (AddedInvoices.Contains(inv)) return;
      edited.Add(inv);
      var newamt = Rng.Next(100, 10000) / 100.0m;
      log.Add($"Acc:{inv.AccountId}({inv.SystemId}) ${inv.Amount:N2}->${newamt:N2}");
      db.Invoices[idx] = inv with { PaidDate = UtcDate.UtcNow.AddDays(Rng.Next(-5, 120)), Amount = newamt, Updated = UtcDate.UtcNow };
    });
    ctx.Debug($"FinSimulation - EditInvoices[{edited.Count}]", log);
    return edited;
  }
}