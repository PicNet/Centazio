using System.Text.Json.Serialization;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Serilog;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  public string Name { get; init; }
  public override string DisplayName => Name;
  public CoreEntityId MembershipCoreId { get; internal init; }
  
  internal CoreCustomer(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, string name, CoreEntityId membershipid) : base(coreid, sysid, system, sourceupdate, system) {
    Name = name;
    MembershipCoreId = membershipid;
  }

  public override object GetChecksumSubset() => new { Name, MembershipId = MembershipCoreId };
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; init; }
  public override string DisplayName => Name;
  
  internal CoreMembershipType(CoreEntityId coreid, SystemEntityId sysid, DateTime sourceupdate, string name) : base(coreid, sysid, SimulationConstants.CRM_SYSTEM, sourceupdate, SimulationConstants.CRM_SYSTEM) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { Name };

}
public record CoreInvoice : CoreEntityBase {
  
  public override string DisplayName => CoreId;
  public CoreEntityId CustomerCoreId { get; private set; }
  public int Cents { get; init; }
  public DateOnly DueDate { get; init; }
  public DateTime? PaidDate { get; init; }
  
  internal CoreInvoice(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid, sysid, system, sourceupdate, system) {
    CustomerCoreId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public override object GetChecksumSubset() => new { CustomerId = CustomerCoreId, Cents, DueDate, PaidDate };
}

public abstract record CoreEntityBase : ICoreEntity {
  public SystemName System { get; }
  public SystemEntityId SystemId { get; set; }
  public CoreEntityId CoreId { get; set; }
  public DateTime DateCreated { get; set; }
  public DateTime DateUpdated { get; set; }
  public DateTime SourceSystemDateUpdated { get; init; }
  public SystemName LastUpdateSystem { get; set; }
  
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, string lastsys) {
    System = system;
    SystemId = sysid;
    SourceSystemDateUpdated = sourceupdate;
        
    CoreId = coreid;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
  }
}

// todo: we need a base class for core storage with Impl methods so a lot of the boilerplate can be handled
// todo: things like setting the DateCreated / DateUpdated should not be left to the implementor
// todo: we a proper AbstractCoreToSystemMapStore to also do validations
public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
  internal List<ICoreEntity> Types { get; } = [];
  internal List<ICoreEntity> Customers { get; } = [];
  internal List<ICoreEntity> Invoices { get; } = [];
  
  public CoreMembershipType? GetMembershipType(CoreEntityId? coreid) => Types.SingleOrDefault(e => e.CoreId == coreid)?.To<CoreMembershipType>();
  public CoreCustomer? GetCustomer(CoreEntityId? coreid) => Customers.SingleOrDefault(e => e.CoreId == coreid)?.To<CoreCustomer>();
  public CoreInvoice? GetInvoice(CoreEntityId? coreid) => Invoices.SingleOrDefault(e => e.CoreId == coreid)?.To<CoreInvoice>();
  
  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityType coretype, DateTime after) {
    var list = GetList(coretype).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();
    // Log.Debug($"CoreStorage.Get Object[{obj}] After[{after:o}] Exclude[{exclude}] Returning[{String.Join(",", list.Select(e => $"{e.DisplayName}({e.Id})"))}]");
    return Task.FromResult(list);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType coretype, List<CoreEntityId> coreids) {
    var full = GetList(coretype);
    var forcores = coreids.Select(id => full.Single(e => e.CoreId == id)).ToList();
    // Log.Debug($"CoreStorage.Get Object[{obj}] CoreIds[{coreids.Count}] Returning[{String.Join(",", forcores.Select(e => $"{e.DisplayName}({e.Id})"))}]");
    return Task.FromResult(forcores);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityType coretype, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.CoreId);
    return (await Get(new("ignore"), coretype, DateTime.MinValue))
        .Where(e => ids.ContainsKey(e.CoreId))
        .ToDictionary(e => e.CoreId, e => ctx.checksum.Checksum(e));
  }
  
  public Task<List<ICoreEntity>> Upsert(CoreEntityType coretype, List<Containers.CoreChecksum> entities) {
    var target = GetList(coretype);
    var (added, updated) = (0, 0);
    var upserted = entities.Select(e => {
      e.Core.DateUpdated = UtcDate.UtcNow;
      var idx = target.FindIndex(e2 => e2.CoreId == e.Core.CoreId);
      if (idx < 0) {
        added++;
        e.Core.DateCreated = UtcDate.UtcNow;
        ctx.Epoch.Add(target.AddAndReturn(e.Core));
      } else {
        updated++;
        ctx.Epoch.Update(target[idx] = e.Core);
      }
      return e.Core;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.Core.DisplayName}({e.Core.CoreId})")) + $"] Created[{added}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }
  
  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  
  private List<ICoreEntity> GetList(CoreEntityType coretype) {
    if (coretype.Value == nameof(CoreMembershipType)) return Types;
    if (coretype.Value == nameof(CoreCustomer)) return Customers;
    if (coretype.Value == nameof(CoreInvoice)) return Invoices;
    throw new NotSupportedException(coretype);
  }
}