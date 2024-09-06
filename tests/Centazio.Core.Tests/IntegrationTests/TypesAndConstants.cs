namespace Centazio.Core.Tests.IntegrationTests;

public static class Constants {
  public static readonly SystemName CrmSystemName = new("CRM");
  public static readonly ObjectName CrmCustomer = new(nameof(CrmCustomer));
  public static readonly LifecycleStage Read = new(nameof(Read));
}

public record CrmCustomer(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime LastUpdate);