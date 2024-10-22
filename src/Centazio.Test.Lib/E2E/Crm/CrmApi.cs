using Centazio.Core;
using C = Centazio.Test.Lib.E2E.SimulationConstants;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmApi {
  
  internal List<CrmMembershipType> MembershipTypes { get; }
  internal List<CrmCustomer> Customers { get; } = new();
  internal List<CrmInvoice> Invoices { get; } = new();
  
  internal CrmSimulation Simulation { get; }
  
  public CrmApi(SimulationCtx ctx) {
    MembershipTypes = [
      new(C.PENDING_MEMBERSHIP_TYPE_ID, UtcDate.UtcNow, "Pending:0"),
      new(Rng.NewGuid(), UtcDate.UtcNow, "Standard:0"),
      new(Rng.NewGuid(), UtcDate.UtcNow, "Silver:0"),
      new(Rng.NewGuid(), UtcDate.UtcNow, "Gold:0")
    ];
    Simulation = new CrmSimulation(ctx, this);
  }

  public Task<List<string>> GetMembershipTypes(DateTime after) => 
      Task.FromResult(MembershipTypes.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<string>> GetCustomers(DateTime after) => 
      Task.FromResult(Customers.Where(e => e.Updated > after).Select(Json.Serialize).ToList());
  
  public Task<List<string>> GetInvoices(DateTime after) => 
      Task.FromResult(Invoices.Where(e => e.Updated > after).Select(Json.Serialize).ToList());

  public Task<List<CrmCustomer>> CreateCustomers(List<CrmCustomer> news) { 
    var created = news.Select(c => c with { CrmCustId = Rng.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
    Customers.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmCustomer>> UpdateCustomers(List<CrmCustomer> updates) {
    return Task.FromResult(updates.Select(c => {
      var idx = Customers.FindIndex(c2 => c2.SystemId == c.SystemId);
      if (idx < 0) throw new Exception();
      var update = c with { Updated = UtcDate.UtcNow };
      return Customers[idx] = update;
    }).ToList());
  }

  public Task<List<CrmInvoice>> CreateInvoices(List<CrmInvoice> news) {
    var created = news.Select(i => i with { CrmInvId = Rng.NewGuid(), Updated = UtcDate.UtcNow }).ToList();
    Invoices.AddRange(created);
    return Task.FromResult(created);
  }
  
  public Task<List<CrmInvoice>> UpdateInvoices(List<CrmInvoice> updates) {
    return Task.FromResult(updates.Select(i => {
      var idx = Invoices.FindIndex(i2 => i2.SystemId == i.SystemId);
      if (idx < 0) throw new Exception();
      var update = i with { Updated = UtcDate.UtcNow };
      return Invoices[idx] = update;
    }).ToList());
  }
}

public record CrmMembershipType(Guid CrmTypeId, DateTime Updated, string Name) : ISystemEntity {

  public SystemEntityId SystemId => new(CrmTypeId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  public object GetChecksumSubset() => new { Name };

}

public record CrmInvoice(Guid CrmInvId, DateTime Updated, Guid CustomerId, int AmountCents, DateOnly DueDate, DateTime? PaidDate = null) : ISystemEntity {

  public SystemEntityId SystemId => new(CrmInvId.ToString());
  public SystemEntityId CustomerSystemId => new(CustomerId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => $"Cust:{CustomerId}({CrmInvId}) {AmountCents}c";
  public object GetChecksumSubset() => new { CustomerId, AmountCents, DueDate, PaidDate };

}

public record CrmCustomer(Guid CrmCustId, DateTime Updated, Guid MembershipTypeId, string Name) : ISystemEntity {

  public SystemEntityId SystemId => new(CrmCustId.ToString());
  public SystemEntityId MembershipTypeSystemId => new(MembershipTypeId.ToString());
  public DateTime LastUpdatedDate => Updated;
  public string DisplayName => Name;
  public object GetChecksumSubset() => new { MembershipTypeId, Name };

}