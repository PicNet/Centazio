using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;
using Serilog;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; private init; }
  public CoreMembershipType Membership { get; internal init; }
  public List<CoreInvoice> Invoices { get; internal init; }
  
  private CoreCustomer(string id, SystemName source, DateTime sourceupdate, string name, CoreMembershipType membership, List<CoreInvoice> invoices, string checksum) : base(id, source, sourceupdate, source, name, checksum) {
    Name = name;
    Membership = membership;
    Invoices = invoices;
  }
  
  public static CoreCustomer FromCrmCustomer(CrmCustomer c, CoreStorage db) {
    var (membership, invoices) = (db.GetMembershipType(c.MembershipTypeId.ToString()), db.GetInvoicesForCustomer(c.Id.ToString()));
    return new CoreCustomer(c.Id.ToString(), SimulationCtx.CRM_SYSTEM, c.Updated, c.Name, membership, invoices, SimulationCtx.Checksum(c));
  }
  
  public static CoreCustomer FromFinAccount(FinAccount a, CoreStorage db) {
    var (pending, invoices) = (db.GetMembershipType(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString()), db.GetInvoicesForCustomer(a.Id.ToString()));
    return new CoreCustomer(a.Id.ToString(), SimulationCtx.FIN_SYSTEM, a.Updated, a.Name, pending, invoices, SimulationCtx.Checksum(a));
  }

  public override object GetChecksumSubset() => new { Name, Membership = Membership.GetChecksumSubset(), Invoices = Invoices.Select(i => i.GetChecksumSubset()).ToList() };

}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; private init; }
  
  private CoreMembershipType(string id, DateTime sourceupdate, string name, string checksum) : base(id, SimulationCtx.CRM_SYSTEM, sourceupdate, SimulationCtx.CRM_SYSTEM, name, checksum) {
    Name = name;
  }
  
  public static CoreMembershipType FromCrmMembershipType(CrmMembershipType m) => new(m.Id.ToString(), m.Updated, m.Name, SimulationCtx.Checksum(m));
  
  public override object GetChecksumSubset() => new { Name };

}
public record CoreInvoice : CoreEntityBase {
  
  public string CustomerId { get; private set; }
  public int Cents { get; private set; }
  public DateOnly DueDate { get; private set; }
  public DateTime? PaidDate { get; private set; }
  
  private CoreInvoice(string id, SystemName source, DateTime sourceupdate, string customerid, int cents, DateOnly due, DateTime? paid, string checksum) : base(id, source, sourceupdate, source, id, checksum) {
    CustomerId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public static CoreInvoice FromCrmInvoice(CrmInvoice i, string corecustid) => new(i.Id.ToString(), SimulationCtx.CRM_SYSTEM, i.Updated, corecustid, i.AmountCents, i.DueDate, i.PaidDate, SimulationCtx.Checksum(i));

  public static CoreInvoice FromFinInvoice(FinInvoice i, string corecustid) => new(i.Id.ToString(), SimulationCtx.FIN_SYSTEM, i.Updated, corecustid, (int) (i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate, SimulationCtx.Checksum(i));
  
  public override object GetChecksumSubset() => new { CustomerId, Cents, DueDate, PaidDate };
}

public abstract record CoreEntityBase : ICoreEntity {
  public string SourceSystem { get; }
  public string SourceId { get; set; }
  public string Id { get; set; }
  public string Checksum { get; protected init; }
  public DateTime DateCreated { get; protected init; }
  public DateTime DateUpdated { get; protected init; }
  public DateTime SourceSystemDateUpdated { get; protected init; }
  public string LastUpdateSystem { get; protected init; }
  
  public string DisplayName { get; protected init; }
  
  public abstract object? GetChecksumSubset();
  
  protected CoreEntityBase(string id, SystemName source, DateTime sourceupdate, string lastsys, string display, string checksum) {
    SourceSystem = source;
    SourceId = id;
    SourceSystemDateUpdated = sourceupdate;
        
    Id = id;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
    
    DisplayName = display;
    Checksum = checksum;
  }
}

public class CoreStorage : ICoreStorage {
  
  internal List<ICoreEntity> Types { get; } = [];
  internal List<ICoreEntity> Customers { get; } = [];
  internal List<ICoreEntity> Invoices { get; } = [];
  
  public CoreMembershipType GetMembershipType(string id) => Types.Single(e => e.Id == id).To<CoreMembershipType>();
  public CoreCustomer GetCustomer(string id) => Customers.Single(e => e.Id == id).To<CoreCustomer>();
  public CoreInvoice GetInvoice(string id) => Invoices.Single(e => e.Id == id).To<CoreInvoice>();
  public List<CoreInvoice> GetInvoicesForCustomer(string id) => Invoices.Cast<CoreInvoice>().Where(e => e.CustomerId == id).ToList();
  
  public Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after, SystemName exclude) => 
      Task.FromResult(GetList(obj).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList());

  public Task<List<ICoreEntity>> Get(CoreEntityType obj, List<string> coreids) {
    var lst = GetList(obj);
    return Task.FromResult(coreids.Select(id => lst.Single(e => e.Id == id)).ToList());
  }

  public async Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.Id);
    return (await Get(obj, DateTime.MinValue, new("ignore")))
        .Where(e => ids.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => Helpers.TestingChecksum(e.GetChecksumSubset() ?? throw new Exception()));
  }
  public Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<CoreEntityAndChecksum> entities) {
    Log.Information($"CoreStorage.Upsert[{obj}] - " + String.Join(",", entities.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.Id})")));
    var target = GetList(obj);
    var upserted = entities.Select(e => {
      var idx = target.FindIndex(e2 => e2.Id == e.CoreEntity.Id);
      if (idx < 0) target.Add(e.CoreEntity);
      else target[idx] = e.CoreEntity;
      return e.CoreEntity;
    }).ToList();
    return Task.FromResult(upserted);
  }
  
  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  
  private List<ICoreEntity> GetList(CoreEntityType obj) {
    if (obj.Value == nameof(CoreMembershipType)) return Types;
    if (obj.Value == nameof(CoreCustomer)) return Customers;
    if (obj.Value == nameof(CoreInvoice)) return Invoices;
    throw new NotSupportedException(obj);
  }
}