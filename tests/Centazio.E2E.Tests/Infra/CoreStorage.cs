using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; private init; }
  public CoreMembershipType Membership { get; internal init; }
  public List<CoreInvoice> Invoices { get; internal init; }
  
  private CoreCustomer(string id, SystemName source, DateTime sourceupdate, string name, CoreMembershipType membership, List<CoreInvoice> invoices, string checksum) : base(id, source, sourceupdate, source, checksum) {
    Name = name;
    Membership = membership;
    Invoices = invoices;
  }
  
  public static CoreCustomer FromCrmCustomer(CrmCustomer c, CoreStorage db) {
    var (id, updated, membership, invoices) = (c.Id.ToString(), c.Updated, db.GetMembershipType(c.MembershipTypeId.ToString()), db.GetInvoicesForCustomer(c.Id.ToString()));
    // var checksum = db.Checksum(new { id, c.Name, Membership = membership.Checksum, Invoices = invoices.Select(e => e.Checksum).ToList() });
    // var checksum = db.Checksum(new { id, c.Name, Membership = membership.Checksum });
    var checksum = db.Checksum(new { id, c.Name });
    Console.WriteLine("FromCrmCustomer: " + c.Name);
    return new CoreCustomer(id, SimulationCtx.CRM_SYSTEM, updated, c.Name, membership, invoices, checksum);
  }
  
  public static CoreCustomer FromFinAccount(FinAccount a, CoreStorage db) {
    var (id, updated, pending, invoices) = (a.Id.ToString(), a.Updated, db.GetMembershipType(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString()), db.GetInvoicesForCustomer(a.Id.ToString()));
    // var checksum = db.Checksum(new { id, a.Name, Invoices = invoices.Select(e => e.Checksum).ToList() });
    var checksum = db.Checksum(new { id, a.Name });
    Console.WriteLine("FromFinAccount: " + a.Name);
    return new CoreCustomer(id, SimulationCtx.FIN_SYSTEM, updated, a.Name, pending, invoices, checksum);
  }
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; private init; }
  
  private CoreMembershipType(string id, DateTime sourceupdate, string name, string checksum) : base(id, SimulationCtx.CRM_SYSTEM, sourceupdate, SimulationCtx.CRM_SYSTEM, checksum) {
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
  
  private CoreInvoice(string id, SystemName source, DateTime sourceupdate, string customerid, int cents, DateOnly due, DateTime? paid, string checksum) : base(id, source, sourceupdate, source, checksum) {
    CustomerId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public static CoreInvoice FromCrmInvoice(CrmInvoice i, CoreStorage db) {
    var (id, updated, customer, cents, due, paid) = (i.Id.ToString(), i.Updated, i.CustomerId.ToString(), i.AmountCents, i.DueDate, i.PaidDate);
    return new CoreInvoice(id, SimulationCtx.CRM_SYSTEM, updated, customer, cents, due, paid, db.Checksum(new { id, customer, cents, due, paid }));
  }
  
  public static CoreInvoice FromFinInvoice(FinInvoice i, CoreStorage db) {
    var (id, updated, account, amt, due, paid) = (i.Id.ToString(), i.Updated, i.AccountId.ToString(), i.Amount, i.DueDate, i.PaidDate);
    return new CoreInvoice(id, SimulationCtx.FIN_SYSTEM, updated, account, (int) (amt * 100), DateOnly.FromDateTime(due), paid, db.Checksum(new { id, customer = account, amt, due, paid }));
  }
}

public abstract record CoreEntityBase : ICoreEntity {
  public string SourceSystem { get; }
  public string SourceId { get; protected init; }
  public string Id { get; protected init; }
  public string Checksum { get; protected init; }
  public DateTime DateCreated { get; protected init; }
  public DateTime DateUpdated { get; protected init; }
  public DateTime SourceSystemDateUpdated { get; protected init; }
  public string LastUpdateSystem { get; protected init; }
  
  protected CoreEntityBase(string id, SystemName source, DateTime sourceupdate, string lastsys, string checksum) {
    SourceSystem = source;
    SourceId = id;
    SourceSystemDateUpdated = sourceupdate;
        
    Id = id;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
    
    Checksum = checksum;
  }
}

public class CoreStorage : ICoreStorageGetter, ICoreStorageUpserter {
  
  internal List<ICoreEntity> Types { get; } = [];
  internal List<ICoreEntity> Customers { get; } = [];
  internal List<ICoreEntity> Invoices { get; } = [];
  
  public string Checksum(object o) => Helpers.TestingChecksum(o);
  
  public CoreMembershipType GetMembershipType(string id) => Types.Single(e => e.Id == id).To<CoreMembershipType>();
  public CoreCustomer GetCustomer(string id) => Customers.Single(e => e.Id == id).To<CoreCustomer>();
  public CoreInvoice GetInvoice(string id) => Invoices.Single(e => e.Id == id).To<CoreInvoice>();
  public List<CoreInvoice> GetInvoicesForCustomer(string id) => Invoices.Cast<CoreInvoice>().Where(e => e.CustomerId == id).ToList();
  
  public Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after, SystemName exclude) => 
      Task.FromResult(GetList(obj).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList());

  public Task<List<ICoreEntity>> Get(CoreEntityType obj, IList<string> coreids) {
    var lst = GetList(obj);
    return Task.FromResult(coreids.Select(id => lst.Single(e => e.Id == id)).ToList());
  }

  public async Task<Dictionary<string, string>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.Id);
    return (await Get(obj, DateTime.MinValue, new("ignore")))
        .Where(e => ids.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => e.Checksum);
  }
  public Task<IEnumerable<ICoreEntity>> Upsert(CoreEntityType obj, IEnumerable<ICoreEntity> entities) {
    if (obj == CoreEntityType.From<CoreCustomer>()) Console.WriteLine("Upserting CoreCustomer: " + String.Join(",", entities.Select(e => ((CoreCustomer)e).Name)));
    var (source, target) = (entities.ToList(), GetList(obj));
    source.ForEach(e => {
      var idx = target.FindIndex(e2 => e2.Id == e.Id);
      if (idx < 0) target.Add(e);
      else target[idx] = e;
    });
    return Task.FromResult<IEnumerable<ICoreEntity>>(source);
  }
  
  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  
  private List<ICoreEntity> GetList(CoreEntityType obj) {
    if (obj.Value == nameof(CoreMembershipType)) return Types;
    if (obj.Value == nameof(CoreCustomer)) return Customers;
    if (obj.Value == nameof(CoreInvoice)) return Invoices;
    throw new NotSupportedException(obj);
  }

}