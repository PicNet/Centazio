using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.IntegrationTests;

public static class Constants {
  public static readonly SystemName System1Name = new("CRM");
  public static readonly SystemName System2Name = new("FIN");
  public static readonly ObjectName CoreEntityName = new(nameof(CoreEntity));
}

public record System1Entity(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated);

public record CoreEntity(string Id, string Checksum, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated) : ICoreEntity {

  public string SourceId { get; init; } = Id;
  public string SourceSystem { get; } = Constants.System1Name;
  public DateTime DateCreated { get; } = DateUpdated;
  public DateTime SourceSystemDateUpdated => DateUpdated;

  public record Dto {
    public string? Id { get; init; }
    public string? Checksum { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public DateTime? DateUpdated { get; init; } 
    public string? SourceSystem { get; init; } 
    public DateTime? DateCreated { get; init; } 
    public DateTime? SourceSystemDateUpdated { get; init; }
    
    public static explicit operator CoreEntity(Dto raw) => new(
        raw.Id ?? throw new ArgumentNullException(nameof(Id)),
        raw.Checksum ?? "",
        raw.FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
        raw.LastName ?? throw new ArgumentNullException(nameof(LastName)),
        raw.DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)),
        raw.DateUpdated ?? UtcDate.UtcNow);
  }
}