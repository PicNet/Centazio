#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.ComponentModel.DataAnnotations;

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
}

public record CoreMembershipType : CoreEntityBase {

  [MaxLength(128)] public string Name { get; init; }
  public override string DisplayName => Name;
  
  private CoreMembershipType() {}
  internal CoreMembershipType(CoreEntityId coreid, string name) : base(coreid) {
    Name = name;
  }
  
  public override object GetChecksumSubset() => new { CoreId, Name };
}

public record CoreInvoice : CoreEntityBase {
  
  public override string DisplayName => CoreId;
  public CoreEntityId CustomerCoreId { get; private init; }
  public int Cents { get; init; }
  public DateOnly DueDate { get; init; }
  public DateTime? PaidDate { get; init; }
  
  private CoreInvoice() {}
  internal CoreInvoice(CoreEntityId coreid, CoreEntityId customerid, int cents, DateOnly due, DateTime? paid) : base(coreid) {
    CustomerCoreId = customerid;
    Cents = cents;
    DueDate = due;
    PaidDate = paid;
  }
  
  public override object GetChecksumSubset() => new { CoreId, CustomerId = CustomerCoreId, Cents, DueDate, PaidDate };
}