#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
  internal CoreCustomer(CoreEntityId coreid, SystemEntityId sysid, string name, CoreEntityId membershipid) : base(coreid, sysid) {
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
  internal CoreMembershipType(CoreEntityId coreid, SystemEntityId sysid, string name) : base(coreid, sysid) {
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
  internal CoreInvoice(CoreEntityId coreid, SystemEntityId sysid, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid, sysid) {
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
  public SystemName System { get; set; }
  public SystemEntityId SystemId { get; set; }
  public CoreEntityId CoreId { get; set; }
  public DateTime DateCreated { get; set; }
  public DateTime DateUpdated { get; set; }
  public SystemName LastUpdateSystem { get; set; }
  
  [JsonIgnore] public abstract string DisplayName { get; }
  public abstract object GetChecksumSubset();
  
  protected CoreEntityBase() {}
  protected CoreEntityBase(CoreEntityId coreid, SystemEntityId sysid) {
    SystemId = sysid;
    CoreId = coreid;
  }
  
  public abstract record Dto<E> where E : CoreEntityBase {
    public string? System { get; init; }
    public string? SystemId { get; init; }
    public string? CoreId { get; init; }
    public DateTime? DateCreated { get; init; }
    public DateTime? DateUpdated { get; init; }
    public string? LastUpdateSystem { get; init; }
    
    protected Dto() {}
    
    internal Dto(string? system, string? systemid, string coreid, DateTime? created, DateTime? updated, string? lastsystem) {
      System = system;
      SystemId = systemid;
      CoreId = coreid; 
      DateCreated = created;
      DateUpdated = updated;
      LastUpdateSystem = lastsystem;
    }
    
    public abstract E ToCoreEntity();
    
    protected E FillBaseProperties(E e) { 
      e.System = new(System ?? throw new ArgumentNullException(nameof(System)));
      e.SystemId = new (SystemId ?? throw new ArgumentNullException(nameof(SystemId)));
      e.CoreId = new (CoreId ?? throw new ArgumentNullException(nameof(CoreId)));
      e.DateCreated = DateCreated ?? throw new ArgumentNullException(nameof(DateCreated));
      e.DateUpdated = DateUpdated ?? throw new ArgumentNullException(nameof(DateUpdated));
      e.LastUpdateSystem = LastUpdateSystem ?? throw new ArgumentNullException(nameof(LastUpdateSystem));
      return e;
    }
  }
}

public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public CoreMembershipType? GetMembershipType(CoreEntityId? coreid) => GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public List<CoreMembershipType> GetMembershipTypes() => GetList<CoreMembershipType, CoreMembershipType.Dto>();
  public CoreCustomer? GetCustomer(CoreEntityId? coreid) => GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public List<CoreCustomer> GetCustomers() => GetList<CoreCustomer, CoreCustomer.Dto>();
  public CoreInvoice? GetInvoice(CoreEntityId? coreid) => GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public List<CoreInvoice> GetInvoices() => GetList<CoreInvoice, CoreInvoice.Dto>();

  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    var list = GetList(coretype).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();
    return Task.FromResult(list);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var full = GetList(coretype);
    var forcores = coreids.Select(id => full.Single(e => e.CoreId == id)).ToList();
    return Task.FromResult(forcores);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      (await Get(new("ignore"), coretype, DateTime.MinValue))
          .Where(e => coreids.Contains(e.CoreId))
          .ToDictionary(e => e.CoreId, e => ctx.ChecksumAlg.Checksum(e));

  public Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<Containers.CoreChecksum> entities) {
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.Core.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.Core.CoreId)) { ctx.Epoch.Update(e.Core); } 
      else { ctx.Epoch.Add(e.Core); }
      
      target[e.Core.CoreId] = Json.Serialize(e.Core);
      return e.Core;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.Core.DisplayName}({e.Core.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  private List<ICoreEntity> GetList(CoreEntityTypeName coretype) {
    if (coretype.Value == nameof(CoreMembershipType)) return GetList<CoreMembershipType, CoreMembershipType.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreCustomer)) return GetList<CoreCustomer, CoreCustomer.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreInvoice)) return GetList<CoreInvoice, CoreInvoice.Dto>().Cast<ICoreEntity>().ToList();
    throw new NotSupportedException(coretype);
  }
  
  private List<E> GetList<E, D>() 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    return db[CoreEntityTypeName.From<E>()].Keys.Select(coreid => GetSingle<E, D>(coreid) ?? throw new Exception()).ToList();
  }

  private E? GetSingle<E, D>(CoreEntityId? coreid) 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (coreid is null || !dict.TryGetValue(coreid, out var json)) return default;

    return (Json.Deserialize<D>(json) ?? throw new Exception()).ToCoreEntity();
  }
}