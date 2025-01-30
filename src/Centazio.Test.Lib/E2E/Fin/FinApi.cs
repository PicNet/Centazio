using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinApi {

  internal List<FinAccount> Accounts { get; } = new();
  internal List<FinInvoice> Invoices { get; } = new();
  internal FinSimulation Simulation { get; }

  public FinApi(SimulationCtx ctx) => Simulation = new FinSimulation(ctx, Accounts, Invoices);
  
  public SystemName System => SimulationConstants.FIN_SYSTEM;
  
  public Task<List<string>> GetAccounts(DateTime after) => Task.FromResult(Accounts.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  public Task<List<string>> GetInvoices(DateTime after) => Task.FromResult(Invoices.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<FinAccount>> CreateAccounts(SimulationCtx ctx, List<FinAccount> news) { 
    var created = news.Select(c => c with { SystemId = ctx.NewIntSeid(), Updated = UtcDate.UtcNow }).ToList();
    Accounts.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinAccount>> UpdateAccounts(List<FinAccount> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = Accounts.FindIndex(c2 => c2.SystemId == c.SystemId);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return Accounts[idx] = update;
    }).ToList());
  }

  public Task<List<FinInvoice>> CreateInvoices(SimulationCtx ctx, List<FinInvoice> news) {
    var created = news.Select(i => i with { SystemId = ctx.NewIntSeid(), Updated = UtcDate.UtcNow }).ToList();
    Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinInvoice>> UpdateInvoices(List<FinInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = Invoices.FindIndex(i2 => i2.SystemId == i.SystemId);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return Invoices[idx] = update;
    }).ToList());
  }
}

public record FinInvoice(SystemEntityId SystemId, SystemEntityId AccountId, decimal Amount, DateTime Updated, DateTime DueDate, DateTime? PaidDate) : ISystemEntity {

  public SystemEntityId AccountSystemId => new(AccountId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => $"Acct:{AccountId.Value}({SystemId.Value}) {Amount}c";
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, AccountId, Amount, DueDate, PaidDate };

}

public record FinAccount(SystemEntityId SystemId, string Name, DateTime Updated) : ISystemEntity {

  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, Name };

}