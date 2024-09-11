using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.IntegrationTests;

public static class Constants {
  public static readonly SystemName CrmSystemName = new("CRM");
  public static readonly ObjectName CrmCustomer = new(nameof(CrmCustomer));
  public static readonly LifecycleStage Read = new(nameof(Read));
  public static readonly LifecycleStage Promote = new(nameof(Promote));
}

public record CrmCustomer(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated);

public record CoreCustomer(string Id, string Checksum, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated) : ICoreEntity {

  public string SourceSystem { get; } = Constants.CrmSystemName;
  public DateTime DateCreated { get; } = DateUpdated;
  public DateTime SourceSystemDateUpdated => DateUpdated;

}