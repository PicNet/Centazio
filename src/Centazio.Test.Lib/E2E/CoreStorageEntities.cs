#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.ComponentModel.DataAnnotations;
using Centazio.Core.CoreRepo;
using Centazio.Core.Types;

namespace Centazio.Test.Lib.E2E;

public record CoreCustomer : CoreEntityBase {
  
  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  public CoreEntityId MembershipCoreId { get; internal init; }
  
  private CoreCustomer() {}
  internal CoreCustomer(CoreEntityId coreid, string name, CoreEntityId membershipid) : base(coreid) {
    Name = name;
    MembershipCoreId = membershipid;
  }

  public override object GetChecksumSubset() => new { CoreId, Name, MembershipId = MembershipCoreId };
  
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
  internal CoreMembershipType(CoreEntityId coreid, string name) : base(coreid) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { CoreId, Name };
  
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
  internal CoreInvoice(CoreEntityId coreid, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid) {
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