using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Serilog;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; init; }
  public CoreMembershipType Membership { get; internal init; }
  public List<CoreInvoice> Invoices { get; internal init; }
  
  internal CoreCustomer(string id, SystemName source, DateTime sourceupdate, string name, CoreMembershipType membership, List<CoreInvoice> invoices) : base(id, source, sourceupdate, source, name) {
    Name = name;
    Membership = membership;
    Invoices = invoices;
  }

  public override object GetChecksumSubset() => new { Name, Membership = Membership.GetChecksumSubset() };
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; private init; }
  
  internal CoreMembershipType(string id, DateTime sourceupdate, string name) : base(id, SimulationConstants.CRM_SYSTEM, sourceupdate, SimulationConstants.CRM_SYSTEM, name) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { Name };

}
public record CoreInvoice : CoreEntityBase {
  
  public string CustomerId { get; private set; }
  public int Cents { get; private set; }
  public DateOnly DueDate { get; private set; }
  public DateTime? PaidDate { get; private set; }
  
  internal CoreInvoice(string id, SystemName source, DateTime sourceupdate, string customerid, int cents, DateOnly due, DateTime? paid) : base(id, source, sourceupdate, source, id) {
    CustomerId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public override object GetChecksumSubset() => new { CustomerId, Cents, DueDate, PaidDate };
}

public abstract record CoreEntityBase : ICoreEntity {
  public string SourceSystem { get; }
  public string SourceId { get; set; }
  public string Id { get; set; }
  public DateTime DateCreated { get; protected init; }
  public DateTime DateUpdated { get; protected init; }
  public DateTime SourceSystemDateUpdated { get; init; }
  public string LastUpdateSystem { get; protected init; }
  
  public string DisplayName { get; }
  
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase(string id, SystemName source, DateTime sourceupdate, string lastsys, string display) {
    SourceSystem = source;
    SourceId = id;
    SourceSystemDateUpdated = sourceupdate;
        
    Id = id;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
    
    DisplayName = display;
  }
}

public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
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

  public async Task<Dictionary<string, CoreEntityChecksum>> GetChecksums(CoreEntityType obj, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.Id);
    return (await Get(obj, DateTime.MinValue, new("ignore")))
        .Where(e => ids.ContainsKey(e.Id))
        .ToDictionary(e => e.Id, e => ctx.checksum.Checksum(e));
  }
  
  public Task<List<ICoreEntity>> Upsert(CoreEntityType obj, List<Containers.CoreChecksum> entities) {
    var target = GetList(obj);
    var (added, updated) = (0, 0);
    var upserted = entities.Select(e => {
      var idx = target.FindIndex(e2 => e2.Id == e.Core.Id);
      if (idx < 0) {
        added++;
        ctx.Epoch.Add(target.AddAndReturn(e.Core));
      } else {
        updated++;
        ctx.Epoch.Update(ctx.CurrentSystem ?? throw new Exception(), target[idx] = e.Core);
      }
      return e.Core;
    }).ToList();
    
    Log.Information($"CoreStorage.Upsert[{obj}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.Core.DisplayName}({e.Core.Id})")) + $"] Created[{added}] Updated[{updated}]");
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