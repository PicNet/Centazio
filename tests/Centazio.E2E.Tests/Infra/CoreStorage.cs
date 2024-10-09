using System.Text.Json.Serialization;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Serilog;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; init; }
  public override string DisplayName => Name;
  public string MembershipId { get; internal init; }
  
  internal CoreCustomer(string id, SystemName source, DateTime sourceupdate, string name, string membershipid) : base(id, source, sourceupdate, source) {
    Name = name;
    MembershipId = membershipid;
  }

  public override object GetChecksumSubset() => new { Name, MembershipId };
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; init; }
  public override string DisplayName => Name;
  
  internal CoreMembershipType(string id, DateTime sourceupdate, string name) : base(id, SimulationConstants.CRM_SYSTEM, sourceupdate, SimulationConstants.CRM_SYSTEM) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { Name };

}
public record CoreInvoice : CoreEntityBase {
  
  public override string DisplayName => Id;
  public string CustomerId { get; private set; }
  public int Cents { get; init; }
  public DateOnly DueDate { get; init; }
  public DateTime? PaidDate { get; init; }
  
  internal CoreInvoice(string id, SystemName source, DateTime sourceupdate, string customerid, int cents, DateOnly due, DateTime? paid) : base(id, source, sourceupdate, source) {
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
  public DateTime DateCreated { get; set; }
  public DateTime DateUpdated { get; set; }
  public DateTime SourceSystemDateUpdated { get; init; }
  public string LastUpdateSystem { get; set; }
  
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase(string id, SystemName source, DateTime sourceupdate, string lastsys) {
    SourceSystem = source;
    SourceId = id;
    SourceSystemDateUpdated = sourceupdate;
        
    Id = id;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
  }
}

// todo: we need a base class for core storage with Impl methods so a lot of the boilerplate can be handled
// todo: things like setting the DateCreated / DateUpdated should not be left to the implementor
public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
  internal List<ICoreEntity> Types { get; } = [];
  internal List<ICoreEntity> Customers { get; } = [];
  internal List<ICoreEntity> Invoices { get; } = [];
  
  public CoreMembershipType GetMembershipType(string id) => Types.Single(e => e.Id == id).To<CoreMembershipType>();
  public CoreCustomer GetCustomer(string id) => Customers.Single(e => e.Id == id).To<CoreCustomer>();
  public CoreInvoice GetInvoice(string id) => Invoices.Single(e => e.Id == id).To<CoreInvoice>();
  public List<CoreInvoice> GetInvoicesForCustomer(string id) => Invoices.Cast<CoreInvoice>().Where(e => e.CustomerId == id).ToList();
  
  public Task<List<ICoreEntity>> Get(CoreEntityType obj, DateTime after, SystemName exclude) {
    var list = GetList(obj).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();
    // Log.Debug($"CoreStorage.Get Object[{obj}] After[{after:o}] Exclude[{exclude}] Returning[{String.Join(",", list.Select(e => $"{e.DisplayName}({e.Id})"))}]");
    return Task.FromResult(list);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType obj, List<ValidString> coreids) {
    var full = GetList(obj);
    var forcores = coreids.Select(id => full.Single(e => e.Id == id)).ToList();
    // Log.Debug($"CoreStorage.Get Object[{obj}] CoreIds[{coreids.Count}] Returning[{String.Join(",", forcores.Select(e => $"{e.DisplayName}({e.Id})"))}]");
    return Task.FromResult(forcores);
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
      e.Core.DateUpdated = UtcDate.UtcNow;
      var idx = target.FindIndex(e2 => e2.Id == e.Core.Id);
      if (idx < 0) {
        added++;
        e.Core.DateCreated = UtcDate.UtcNow;
        ctx.Epoch.Add(target.AddAndReturn(e.Core));
      } else {
        updated++;
        ctx.Epoch.Update(e.Core.LastUpdateSystem, target[idx] = e.Core);
      }
      return e.Core;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{obj}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.Core.DisplayName}({e.Core.Id})")) + $"] Created[{added}] Updated[{updated}]");
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