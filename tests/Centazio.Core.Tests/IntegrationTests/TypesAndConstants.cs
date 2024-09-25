using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.IntegrationTests;

public static class Constants {
  public static readonly SystemName System1Name = new("CRM");
  public static readonly SystemName System2Name = new("FIN");
  public static readonly ObjectName System1Entity = new(nameof(System1Entity));
}

public record System1Entity(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated);

public record CoreEntity(string Id, string Checksum, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated) : ICoreEntity {

  public string SourceId { get; init; } = Id;
  public string SourceSystem { get; } = Constants.System1Name;
  public DateTime DateCreated { get; } = DateUpdated;
  public DateTime SourceSystemDateUpdated => DateUpdated;

}