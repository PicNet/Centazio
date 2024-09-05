namespace Centazio.Core.Tests.DummySystems;

public static class Constants {
  public static readonly SystemName CrmSystemName = new(nameof(Crm));
  public static readonly ObjectName CrmCustomer = new(nameof(CrmCustomer));
  public static readonly LifecycleStage Read = new(nameof(Read));
}

public record CrmCustomer(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime LastUpdate);