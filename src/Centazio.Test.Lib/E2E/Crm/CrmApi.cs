namespace Centazio.Test.Lib.E2E.Crm;

public class CrmDb {
  internal List<CrmMembershipType> MembershipTypes { get; } = new();
  internal List<CrmCustomer> Customers { get; } = new();
  internal List<CrmInvoice> Invoices { get; } = new();
}

public class CrmApi(CrmDb db) {

  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(db.MembershipTypes.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(db.Customers.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(db.Invoices.Where(e => e.Updated > after).Select(Json.Serialize).ToList());

  public Task<List<CrmCustomer>> CreateCustomers(SimulationCtx ctx, List<CrmCustomer> news) { 
    var created = news.Select(c => c with { SystemId = ctx.NewGuidSeid(), Updated = UtcDate.UtcNow }).ToList();
    db.Customers.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmCustomer>> UpdateCustomers(List<CrmCustomer> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = db.Customers.FindIndex(c2 => c2.SystemId == c.SystemId);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return db.Customers[idx] = update;
    }).ToList());
  }

  public Task<List<CrmInvoice>> CreateInvoices(SimulationCtx ctx, List<CrmInvoice> news) {
    var created = news.Select(i => i with { SystemId = ctx.NewGuidSeid(), Updated = UtcDate.UtcNow }).ToList();
    db.Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmInvoice>> UpdateInvoices(List<CrmInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = db.Invoices.FindIndex(i2 => i2.SystemId == i.SystemId);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return db.Invoices[idx] = update;
    }).ToList());
  }
}

public record CrmMembershipType(SystemEntityId SystemId, DateTime Updated, string Name) : ISystemEntity {

  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, Name };

}

public record CrmInvoice(SystemEntityId SystemId, DateTime Updated, SystemEntityId CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null) : ISystemEntity {

  public SystemEntityId CustomerSystemId => new(CustomerId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => $"Cust:{CustomerId}({SystemId.Value}) {AmountCents}c";
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, CustomerId, AmountCents, DueDate, PaidDate };

}

public record CrmCustomer(SystemEntityId SystemId, DateTime Updated, SystemEntityId MembershipTypeId, string Name) : ISystemEntity {
  
  public SystemEntityId MembershipTypeSystemId => new(MembershipTypeId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { SystemId = newid };
  public object GetChecksumSubset() => new { SystemId, MembershipTypeId, Name };

}