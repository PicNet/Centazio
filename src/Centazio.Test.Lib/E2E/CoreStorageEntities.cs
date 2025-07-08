#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.ComponentModel.DataAnnotations;

namespace Centazio.Test.Lib.E2E;

public record CoreCustomer : CoreEntityBase {
  
  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  public CoreEntityId MembershipCoreId { get; internal init; }
  
  private CoreCustomer() {}
  internal CoreCustomer(CoreEntityId coreid, CorrelationId correlationid, string name, CoreEntityId membershipid) : base(coreid, correlationid) {
    Name = name;
    MembershipCoreId = membershipid;
  }

  public override object GetChecksumSubset() => new { CoreId, Name, MembershipId = MembershipCoreId };
  
  public record Dto : Dto<CoreCustomer> {
    public string? Name { get; init; }
    public string? MembershipCoreId { get; init; }
    
    
    public override CoreCustomer ToBase() => new() {
      CoreId = new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      CorrelationId = new(CorrelationId ?? throw new ArgumentNullException(nameof(CorrelationId))),
      Name = new(Name ?? throw new ArgumentNullException(nameof(Name))),
      MembershipCoreId = new(MembershipCoreId ?? throw new ArgumentNullException(nameof(MembershipCoreId)))
    };

  }
}

public record CoreMembershipType : CoreEntityBase {

  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  
  private CoreMembershipType() {}
  internal CoreMembershipType(CoreEntityId coreid, CorrelationId correlationid, string name) : base(coreid, correlationid) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { CoreId, Name };
  
  public record Dto : Dto<CoreMembershipType> {
    public string? Name { get; init; }
    
    public override CoreMembershipType ToBase() => new() {
      CoreId = new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      CorrelationId = new(CorrelationId ?? throw new ArgumentNullException(nameof(CorrelationId))),
      Name = new(Name ?? throw new ArgumentNullException(nameof(Name))) 
    };

  }
}

public record CoreInvoice : CoreEntityBase {
  
  public override string DisplayName => CoreId;
  public CoreEntityId CustomerCoreId { get; private init; }
  public int Cents { get; init; }
  public DateOnly DueDate { get; init; }
  public DateTime? PaidDate { get; init; }
  
  private CoreInvoice() {}
  internal CoreInvoice(CoreEntityId coreid, CorrelationId correlationid, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid, correlationid) {
    CustomerCoreId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public override object GetChecksumSubset() => new { CoreId, CustomerId = CustomerCoreId, Cents, DueDate, PaidDate };
  
  public record Dto : Dto<CoreInvoice> {
    public string? CustomerCoreId { get; init; }
    public int? Cents { get; init; }
    public DateOnly? DueDate { get; init; }
    public DateTime? PaidDate { get; init; }
    
    public override CoreInvoice ToBase() => new() {
      CoreId = new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
      CorrelationId = new(CorrelationId ?? throw new ArgumentNullException(nameof(CorrelationId))),
      CustomerCoreId = new(CustomerCoreId ?? throw new ArgumentNullException(nameof(CustomerCoreId))),
      Cents = Cents ?? throw new ArgumentNullException(nameof(Cents)),
      DueDate = DueDate ?? throw new ArgumentNullException(nameof(DueDate)),
      PaidDate = PaidDate
    };

  }
}