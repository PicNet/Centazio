using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; private init; }
  public CoreMembershipType Membership { get; private init; }
  public List<CoreInvoice> Invoices { get; private init; }
  
  private CoreCustomer(string id, DateTime sourceupdate, string name, CoreMembershipType membership, List<CoreInvoice> invoices, string checksum) : base(id, sourceupdate, checksum) {
    Name = name;
    Membership = membership;
    Invoices = invoices;
  }
  
  public static CoreCustomer FromCrmCustomer(CrmCustomer c, CoreStorage db) {
    var (id, updated, membership, invoices) = (c.Id.ToString(), c.Updated, db.GetMembershipType(c.MembershipTypeId.ToString()), db.GetInvoicesForCustomer(c.Id.ToString()));
    var checksum = db.Checksum(new { id, c.Name, Membership = membership.Checksum, Invoices = invoices.Select(e => e.Checksum).ToList() });
    return new CoreCustomer(id, updated, c.Name, membership, invoices, checksum);
  }
  
  public static CoreCustomer FromFinAccount(FinAccount a, CoreStorage db) {
    var (id, updated, pending, invoices) = (a.Id.ToString(), a.Updated, db.GetMembershipType(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString()), db.GetInvoicesForCustomer(a.Id.ToString()));
    var checksum = db.Checksum(new { id, a.Name, Invoices = invoices.Select(e => e.Checksum).ToList() });
    return new CoreCustomer(id, updated, a.Name, pending, invoices, checksum);
  }
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; private init; }
  
  private CoreMembershipType(string id, DateTime sourceupdate, string name, string checksum) : base(id, sourceupdate, checksum) {
    Name = name;
  }
  
  public static CoreMembershipType FromCrmMembershipType(CrmMembershipType m, CoreStorage db) {
    var (id, updated, name) = (m.Id.ToString(), m.Updated, m.Name);
    return new CoreMembershipType(id, updated, name, db.Checksum(new { id, name }));
  }

}
public record CoreInvoice : CoreEntityBase {
  
  public string CustomerId { get; private set; }
  public int Cents { get; private set; }
  public DateOnly DueDate { get; private set; }
  public DateTime? PaidDate { get; private set; }
  
  private CoreInvoice(string id, DateTime sourceupdate, string customerid, int cents, DateOnly due, DateTime? paid, string checksum) : base(id, sourceupdate, checksum) {
    CustomerId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public static CoreInvoice FromCrmInvoice(CrmInvoice i, CoreStorage db) {
    var (id, updated, customer, cents, due, paid) = (i.Id.ToString(), i.Updated, i.CustomerId.ToString(), i.AmountCents, i.DueDate, i.PaidDate);
    return new CoreInvoice(id, updated, customer, cents, due, paid, db.Checksum(new { id, customer, cents, due, paid }));
  }
  
  public static CoreInvoice FromFinInvoice(FinInvoice i, CoreStorage db) {
    var (id, updated, account, amt, due, paid) = (i.Id.ToString(), i.Updated, i.AccountId.ToString(), i.Amount, i.DueDate, i.PaidDate);
    return new CoreInvoice(id, updated, account, (int) (amt * 100), DateOnly.FromDateTime(due), paid, db.Checksum(new { id, customer = account, amt, due, paid }));
  }
}

public abstract record CoreEntityBase : ICoreEntity {
  public string SourceSystem { get; } = nameof(CrmSystem);
  public string SourceId { get; protected init; }
  public string Id { get; protected init; }
  public string Checksum { get; protected init; }
  public DateTime DateCreated { get; protected init; }
  public DateTime DateUpdated { get; protected init; }
  public DateTime SourceSystemDateUpdated { get; protected init; }
  
  protected CoreEntityBase(string id, DateTime sourceupdate, string checksum) {
    SourceId = id;
    SourceSystemDateUpdated = sourceupdate;
        
    Id = id;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    
    Checksum = checksum;
  }
}

public class CoreStorage : ICoreStorageGetter, ICoreStorageUpserter {
  
  internal List<ICoreEntity> Types => [];
  internal List<ICoreEntity> Customers => [];
  internal List<ICoreEntity> Invoices => [];
  
  public string Checksum(object o) => Helpers.TestingChecksum(o);
  
  public CoreMembershipType GetMembershipType(string id) => (CoreMembershipType) Types.Single(e => e.Id == id); 
  public CoreCustomer GetCustomer(string id) => (CoreCustomer) Customers.Single(e => e.Id == id);
  public CoreInvoice GetInvoice(string id) => (CoreInvoice) Invoices.Single(e => e.Id == id);
  public List<CoreInvoice> GetInvoicesForCustomer(string id) => Invoices.Cast<CoreInvoice>().Where(e => e.CustomerId == id).ToList();
  
  public Task<List<ICoreEntity>> Get(ObjectName obj, DateTime after) => 
      Task.FromResult(GetList(obj).Where(e => e.DateUpdated > after).ToList());

  public async Task<Dictionary<string, string>> GetChecksums(ObjectName obj, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.Id);
    return (await Get(obj, DateTime.MinValue))
        .Where(e => ids.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => e.Checksum);
  }
  public Task<IEnumerable<ICoreEntity>> Upsert(ObjectName obj, IEnumerable<ICoreEntity> entities) {
    var lst = GetList(obj);
    return Task.FromResult(entities.Select(e => {
      var idx = lst.FindIndex(e2 => e2.Id == e.Id);
      if (idx < 0) lst.Add(e);
      else lst[idx] = e;
      return e;
    }));
  }
  
  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  
  private List<ICoreEntity> GetList(ObjectName obj) {
    if (obj == nameof(CoreMembershipType)) return Types;
    if (obj == nameof(CoreCustomer)) return Customers;
    if (obj == nameof(CoreInvoice)) return Invoices;
    throw new NotSupportedException($"ObjectName[{obj}] is not supported");
  }

}