#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.E2E.Tests.Infra;

public record CoreCustomer : CoreEntityBase {
  
  [MaxLength(128)] public string Name { get; init; }
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
    
    public override CoreCustomer ToBase() {
      var target = new CoreCustomer { 
        Name = new(Name ?? throw new ArgumentNullException(nameof(Name))),
        MembershipCoreId = new(MembershipCoreId ?? throw new ArgumentNullException(nameof(MembershipCoreId)))
      };
      return FillBaseProperties(target);
    }
  }
}

public record CoreMembershipType : CoreEntityBase {

  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  
  private CoreMembershipType() {}
  internal CoreMembershipType(CoreEntityId coreid, SystemEntityId sysid, string name) : base(coreid, sysid) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { Name };
  
  public record Dto : Dto<CoreMembershipType> {
    public string? Name { get; init; }
    
    public override CoreMembershipType ToBase() {
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
    
    public override CoreInvoice ToBase() {
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
  
  public abstract record Dto<E> : IDto<E> 
      where E : CoreEntityBase {
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
    
    public abstract E ToBase();
    
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