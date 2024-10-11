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
  
  private CoreCustomer() {}
  internal CoreCustomer(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, string name, CoreEntityId membershipid) : base(coreid, sysid, system, sourceupdate, system) {
    Name = name;
    MembershipCoreId = membershipid;
  }

  public override object GetChecksumSubset() => new { Name, MembershipId = MembershipCoreId };
  
  public record Dto : Dto<CoreCustomer> {
    public string? Name { get; init; }
    public string? MembershipCoreId { get; init; }
    
    public override CoreCustomer ToCoreEntity() {
      var target = new CoreCustomer { 
        Name = new(Name ?? throw new ArgumentNullException(nameof(Name))),
        MembershipCoreId = new(MembershipCoreId ?? throw new ArgumentNullException(nameof(MembershipCoreId)))
      };
      return FillBaseProperties(target);
    }
  }
}

public record CoreMembershipType : CoreEntityBase {

  public string Name { get; init; }
  public override string DisplayName => Name;
  
  private CoreMembershipType() {}
  internal CoreMembershipType(CoreEntityId coreid, SystemEntityId sysid, DateTime sourceupdate, string name) : base(coreid, sysid, SimulationConstants.CRM_SYSTEM, sourceupdate, SimulationConstants.CRM_SYSTEM) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { Name };
  
  public record Dto : Dto<CoreMembershipType> {
    public string? Name { get; init; }
    
    public override CoreMembershipType ToCoreEntity() {
      var target = new CoreMembershipType { Name = new(Name ?? throw new ArgumentNullException(nameof(Name))) };
      return FillBaseProperties(target);
    }
  }
}
public record CoreInvoice : CoreEntityBase {
  
  public override string DisplayName => CoreId;
  public CoreEntityId CustomerCoreId { get; private set; }
  public int Cents { get; set; }
  public DateOnly DueDate { get; set; }
  public DateTime? PaidDate { get; set; }
  
  private CoreInvoice() {}
  internal CoreInvoice(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid, sysid, system, sourceupdate, system) {
    CustomerCoreId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public override object GetChecksumSubset() => new { CustomerId = CustomerCoreId, Cents, DueDate, PaidDate };
  
  public record Dto : Dto<CoreInvoice> {
    public string? CustomerCoreId { get; init; }
    public int? Cents { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTime? PaidDate { get; init; }
    
    public override CoreInvoice ToCoreEntity() {
      var target = new CoreInvoice {
        CustomerCoreId = new(CustomerCoreId ?? throw new ArgumentNullException(nameof(CustomerCoreId))),
        Cents = Cents ?? throw new ArgumentNullException(nameof(Cents)),
        DueDate = DueDate ?? throw new ArgumentNullException(nameof(DueDate)),
        PaidDate = PaidDate
      };
      return FillBaseProperties(target);
    }
  }
}

public abstract record CoreEntityBase : ICoreEntity {
  public SystemName System { get; private set; }
  public SystemEntityId SystemId { get; set; }
  public CoreEntityId CoreId { get; set; }
  public DateTime DateCreated { get; set; }
  public DateTime DateUpdated { get; set; }
  public DateTime SourceSystemDateUpdated { get; private set; }
  public SystemName LastUpdateSystem { get; set; }
  
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() {}
  protected CoreEntityBase(CoreEntityId coreid, SystemEntityId sysid, SystemName system, DateTime sourceupdate, string lastsys) {
    System = system;
    SystemId = sysid;
    SourceSystemDateUpdated = sourceupdate;
        
    CoreId = coreid;
    DateCreated = UtcDate.UtcNow;
    DateUpdated = UtcDate.UtcNow;
    LastUpdateSystem = lastsys;
  }
  
  public abstract record Dto<E> where E : CoreEntityBase {
    public string? System { get; init; }
    public string? SystemId { get; init; }
    public string? CoreId { get; init; }
    public DateTime? DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
    public DateTime? SourceSystemDateUpdated { get; init; } // todo: rename SourceSystem -> System
    public string? LastUpdateSystem { get; init; }
    
    protected Dto() {}
    
    internal Dto(string? system, string? systemid, string coreid, DateTime? created, DateTime? updated, DateTime? systemupdated, string? lastsystem) {
      System = system;
      SystemId = systemid;
      CoreId = coreid; 
      DateCreated = created;
      DateUpdated = updated;
      SourceSystemDateUpdated = systemupdated;
      LastUpdateSystem = lastsystem;
    }
    
    public abstract E ToCoreEntity();
    
    protected E FillBaseProperties(E e) { 
      e.System = new(System ?? throw new ArgumentNullException(nameof(System)));
      e.SystemId = new (SystemId ?? throw new ArgumentNullException(nameof(SystemId)));
      e.CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId)));
      e.DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated));
      e.DateUpdated = DateUpdated ?? throw new ArgumentNullException(nameof(DateUpdated));
      e.SourceSystemDateUpdated = SourceSystemDateUpdated ?? throw new ArgumentNullException(nameof(SourceSystemDateUpdated));
      e.LastUpdateSystem = LastUpdateSystem ?? throw new ArgumentNullException(nameof(LastUpdateSystem));
      return e;
    }
  }
}

// todo: we need a base class for core storage with Impl methods so a lot of the boilerplate can be handled
// todo: things like setting the DateCreated / DateUpdated should not be left to the implementor
// todo: we a proper AbstractCoreToSystemMapStore to also do validations
public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
  private readonly Dictionary<CoreEntityType, Dictionary<CoreEntityId, string>> db = new();
  
  public CoreMembershipType? GetMembershipType(CoreEntityId? coreid) => GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public List<CoreMembershipType> GetMembershipTypes() => GetList<CoreMembershipType, CoreMembershipType.Dto>();
  public CoreCustomer? GetCustomer(CoreEntityId? coreid) => GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public List<CoreCustomer> GetCustomers() => GetList<CoreCustomer, CoreCustomer.Dto>();
  public CoreInvoice? GetInvoice(CoreEntityId? coreid) => GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public List<CoreInvoice> GetInvoices() => GetList<CoreInvoice, CoreInvoice.Dto>();

  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityType coretype, DateTime after) {
    var list = GetList(coretype).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();
    return Task.FromResult(list);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityType coretype, List<CoreEntityId> coreids) {
    var full = GetList(coretype);
    var forcores = coreids.Select(id => full.Single(e => e.CoreId == id)).ToList();
    return Task.FromResult(forcores);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityType coretype, List<ICoreEntity> entities) {
    var ids = entities.ToDictionary(e => e.CoreId);
    return (await Get(new("ignore"), coretype, DateTime.MinValue))
        .Where(e => ids.ContainsKey(e.CoreId))
        .ToDictionary(e => e.CoreId, e => ctx.checksum.Checksum(e));
  }

  public Task<List<ICoreEntity>> Upsert(CoreEntityType coretype, List<Containers.CoreChecksum> entities) {
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.Core.CoreId));
    var upserted = entities.Select(e => {
      // todo: date updated / date created should be handled somewhere abstract to allow users not to worry about it 
      e.Core.DateUpdated = UtcDate.UtcNow;
      if (target.ContainsKey(e.Core.CoreId)) {
        ctx.Epoch.Update(e.Core);
      } else {
        e.Core.DateCreated = UtcDate.UtcNow;
        ctx.Epoch.Add(e.Core);
      }
      target[e.Core.CoreId] = Json.Serialize(e.Core);
      return e.Core;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.Core.DisplayName}({e.Core.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  private List<ICoreEntity> GetList(CoreEntityType coretype) {
    if (coretype.Value == nameof(CoreMembershipType)) return GetList<CoreMembershipType, CoreMembershipType.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreCustomer)) return GetList<CoreCustomer, CoreCustomer.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreInvoice)) return GetList<CoreInvoice, CoreInvoice.Dto>().Cast<ICoreEntity>().ToList();
    throw new NotSupportedException(coretype);
  }
  
  private List<E> GetList<E, D>() 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var coretype = CoreEntityType.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    return db[CoreEntityType.From<E>()].Keys.Select(coreid => GetSingle<E, D>(coreid) ?? throw new Exception()).ToList();
  }

  private E? GetSingle<E, D>(CoreEntityId? coreid) 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var dict = db[CoreEntityType.From<E>()];
    if (coreid is null || !dict.TryGetValue(coreid, out var json)) return default;

    return (Json.Deserialize<D>(json) ?? throw new Exception()).ToCoreEntity();
  }
}