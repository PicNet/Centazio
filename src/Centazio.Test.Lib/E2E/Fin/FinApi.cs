namespace Centazio.Test.Lib.E2E.Fin;

public class FinDb {
  public List<FinAccount> Accounts { get; } = [];
  public List<FinInvoice> Invoices { get; } = [];
}

public class FinApi(FinDb db) {

  public Task<List<string>> GetAccounts(DateTime after) => Task.FromResult(db.Accounts.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  public Task<List<string>> GetInvoices(DateTime after) => Task.FromResult(db.Invoices.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<FinAccount>> CreateAccounts(SimulationCtx ctx, List<FinAccount> news) { 
    var created = news.Select(c => c with { SystemId = ctx.NewIntSeid(), Updated = UtcDate.UtcNow }).ToList();
    db.Accounts.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinAccount>> UpdateAccounts(List<FinAccount> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = db.Accounts.FindIndex(c2 => c2.SystemId == c.SystemId);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return db.Accounts[idx] = update;
    }).ToList());
  }

  public Task<List<FinInvoice>> CreateInvoices(SimulationCtx ctx, List<FinInvoice> news) {
    var created = news.Select(i => i with { SystemId = ctx.NewIntSeid(), Updated = UtcDate.UtcNow }).ToList();
    db.Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<FinInvoice>> UpdateInvoices(List<FinInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = db.Invoices.FindIndex(i2 => i2.SystemId == i.SystemId);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return db.Invoices[idx] = update;
    }).ToList());
  }
}

public record FinInvoice(SystemEntityId SystemId, CorrelationId CorrelationId, SystemEntityId AccountId, decimal Amount, DateTime Updated, DateTime DueDate, DateTime? PaidDate) : ISystemEntity {

  public SystemEntityId AccountSystemId => new(AccountId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => $"Acct:{AccountId.Value}({SystemId.Value}) {Amount}c";
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, AccountId, Amount, DueDate, PaidDate };

}

public record FinAccount(SystemEntityId SystemId, CorrelationId CorrelationId, string Name, DateTime Updated) : ISystemEntity {

  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, Name };

}